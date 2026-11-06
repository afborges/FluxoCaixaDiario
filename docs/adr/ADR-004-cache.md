# ADR-004 — Redis com Cache-Aside no Serviço de Consolidado

| Campo | Valor |
|-------|-------|
| **ID** | ADR-004 |
| **Título** | Redis com padrão Cache-Aside para atender 50 req/s no Consolidado |
| **Status** | ✅ Aprovado |
| **Data** | Junho 2026 |

---

## Contexto

O requisito não funcional RNF-02 estabelece:

> *"Em dias de picos, o serviço de consolidado diário recebe 50 requisições por segundo, com no máximo 5% de perda de requisições."*

A consulta de consolidado diário é **read-heavy** e o dado muda com baixa frequência durante o dia (apenas quando um novo lançamento é processado). Isso cria um cenário ideal para cache.

---

## Análise de Capacidade sem Cache

```
50 req/s × 50ms (tempo médio query SQL) = 2.500ms de fila
Para atender 50 req/s com P99 < 200ms precisaríamos de:
50 req/s × 50ms = 2.5 requisições processadas em paralelo
Isso requereria dimensionamento excessivo do banco de dados
```

---

## Decisão

**Redis 7** com padrão **Cache-Aside** para as consultas de consolidado diário.

- Chave: `consolidado:{yyyy-MM-dd}`
- TTL: 300 segundos (5 minutos)
- Invalidação: quando `LancamentoCriadoConsumer` processa um evento, invalida o cache do dia correspondente
- Fallback: se Redis estiver indisponível (circuit breaker aberto), consulta o banco diretamente

---

## Alternativas Consideradas

### IMemoryCache (in-process) ❌
- ✅ Zero latência, sem infraestrutura adicional
- ❌ Cache não é compartilhado entre réplicas (inconsistência se houver 2+ instâncias)
- ❌ Cache perdido a cada restart do pod
- ❌ Não atende o requisito de escalonamento horizontal

### IDistributedCache com SQL Server ❌
- ✅ Sem nova infraestrutura
- ❌ SQL Server não é adequado como cache (latência similar à query original)
- ❌ Derrota o propósito de ter cache

### Memcached ❌
- ✅ Mais simples que Redis, muito rápido
- ❌ Sem suporte a TTL por chave flexível
- ❌ Sem pub/sub (útil em evoluções futuras)
- ❌ Menos adoção no ecossistema .NET

### Redis 7 com Cache-Aside ✅ **ESCOLHIDA**
- ✅ >100.000 ops/s → aguenta o pico de 50 req/s com folga de 2.000x
- ✅ TTL por chave, evita dados stale
- ✅ Compartilhado entre réplicas do Consolidado API
- ✅ Padrão Cache-Aside é simples e transparente para a aplicação
- ✅ Suporte a fallback gracioso quando Redis cair (Polly Circuit Breaker)

---

## Trade-off Decisório

| Trade-off | Impacto | Por que foi aceito |
|-----------|---------|--------------------|
| **Dado pode ter até 5 min de atraso** (TTL após falha de invalidação) | Médio | Aceitável: o consolidado é lido como relatório, não como saldo em tempo real. A invalidação explícita após cada lançamento garante que o caso normal seja consistente. |
| **Redis = mais infraestrutura** para operar | Médio | Sem cache, SQL Server precisaria ser dimensionado para 50 req/s direto. O custo de operar Redis (container leve, ~50MB RAM) é menor que o custo de escalar o banco. |
| **Inconsistência entre réplicas** sem Redis (IMemoryCache) | Alto | Esse foi o fator eliminatório do IMemoryCache: com 2+ réplicas do Consolidado API, cada instância teria seu próprio cache — invalidações em uma réplica não se propagariam às outras. |
| **Cache stampede** se Redis cair e muitos MISS simultâneos | Baixo | Mitigado por degradação graciosa: RedisCacheService retorna `null` em falha, o sistema continua funcionando direto no banco. |
| **Segredo de estado** distribuído | Baixo | Cache armazena apenas o DTO de leitura (sem dados sensíveis crus). O banco permanece a fonte da verdade para auditoria e consistência. |

**O trade-off que definiu a escolha:** IMemoryCache foi descartado por falhar em escalonamento horizontal — condição inegociável para um sistema read-heavy. O custo de consistência eventual (máx 5 min de stale data) foi negociado com o negócio e aceito explicitamente.

---

## Resiliência — 4 Políticas Polly Implementadas

O `RedisCacheService` utiliza um `ResiliencePipeline` do Polly 8 com 4 políticas em cadeia (da mais externa para a mais interna):

| Ordem | Política | Configuração |
|-------|----------|---------------|
| 1ª (ext.) | **Bulkhead** (Concurrency Limiter) | Máx. 20 concurrent; fila de 10 |
| 2ª | **Circuit Breaker** | 50% falhas em 10s, 5 req mínimo; break de 30s |
| 3ª | **Timeout** | 2 segundos por operação |
| 4ª (int.) | **Retry** | 2 tentativas, backoff exponencial 200ms base |

Se o circuit breaker estiver aberto ou qualquer política rejeitar a operação, o `RedisCacheService` retorna `null` com graceful degradation (o sistema consulta o banco diretamente).

---

## Cálculo de Eficiência com Cache

```
Taxa de atualização do consolidado: ~20 lançamentos/dia
Cache TTL: 5 minutos = 288 TTL cycles/dia
Cache invalidations/dia: ~20 (uma por lançamento)

Cache Hit Rate estimado: ~95%+ em dias normais

Com cache:
  - 95% das 50 req/s → Redis (~5ms) = 47.5 req/s servidas em ~5ms
  - 5% das 50 req/s → DB (~50ms) = 2.5 req/s direto ao banco
  - Consultas efetivas ao DB: 2.5/s (muito abaixo da capacidade)
  - RNF de 5% de perda: atendido com folga
```

---

## Consequências

**Positivas:**
- RNF-02 atendido com margem de ~2.000x de capacidade
- Redução de ~95% de carga no banco de dados
- Respostas em ~5ms para cache hit

**Negativas:**
- Dado pode estar desatualizado por até 5 minutos (em caso de falha na invalidação)
- Mais um componente de infraestrutura (Redis)

---

## Histórico de Revisões

| Versão | Data | Autor | Descrição |
|--------|------|-------|-----------|
| 1.0 | Junho 2026 | Arquiteto de Soluções | Decisão inicial — MVP |
| 1.1 | Junho 2026 | Arquiteto de Soluções | Adicionada seção de trade-offs explícita |
| 1.2 | Junho 2026 | Arquiteto de Soluções | Adicionadas 4 políticas Polly: Bulkhead + Circuit Breaker + Timeout (2s) + Retry (2x exp.) |
- Necessidade de Circuit Breaker para quando Redis cair