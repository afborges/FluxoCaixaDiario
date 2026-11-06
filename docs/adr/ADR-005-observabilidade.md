# ADR-005 — OpenTelemetry + Serilog + Prometheus para Observabilidade

| Campo | Valor |
|-------|-------|
| **ID** | ADR-005 |
| **Título** | Stack de Observabilidade: OpenTelemetry + Serilog + Prometheus/Grafana |
| **Status** | ✅ Aprovado |
| **Data** | Junho 2026 |

---

## Contexto

O sistema financeiro precisa de observabilidade robusta para:
- Diagnóstico de problemas em produção (logs estruturados)
- Monitoramento de SLAs (métricas de latência, throughput, taxa de erro)
- Rastreamento de requisições entre serviços (traces distribuídos)
- Alertas proativos (saldo negativo, queue depth alto, latência elevada)

---

## Decisão

Stack de observabilidade open-source, vendor-neutral, baseada em padrões CNCF:

| Pilar | Ferramenta | Protocolo | Status |
|-------|-----------|-----------|--------|
| **Logs** | Serilog → Seq | HTTP Structured Logs | ✅ Implementado |
| **Métricas** | OpenTelemetry → Prometheus → Grafana | `/metrics` HTTP scrape | ✅ Implementado |
| **Traces (local)** | OpenTelemetry → OTLP Exporter | OTLP gRPC/HTTP | ✅ Implementado (exportador configurado) |
| **Traces (distribuídos)** | Jaeger | OTLP gRPC | ⚠️ **NÃO implementado nesta fase** — ver nota abaixo |

> ⚠️ **Jaeger não foi implementado nesta fase do desafio.** A instrumentação via OpenTelemetry está presente e o exportador OTLP está configurado (`AddOtlpExporter`), portanto adicionar o Jaeger é apenas uma questão de subir o container e apontar o endpoint. A razão da não-inclusão nesta fase foi restrição de tempo do desafio. Está previsto no **roadmap Fase 2**.

---

## Alternativas Consideradas

### DataDog ❌
- ✅ Solução completa (logs + métricas + traces + alertas) em uma plataforma
- ✅ Excelente UX, fácil de configurar
- ❌ Custo elevado: ~$30-40/host/mês + por volume de logs
- ❌ Vendor lock-in (migração difícil)
- ❌ Dados financeiros saindo da infraestrutura própria (compliance)

### New Relic ❌
- Mesmo trade-off do DataDog (custo + vendor lock-in)

### ELK Stack (Elasticsearch + Logstash + Kibana) ❌
- ✅ Poderoso, muito adotado para logs
- ❌ Alto consumo de memória/CPU (Elasticsearch requer 2GB+ RAM)
- ❌ Complexidade de operação para o porte desta solução
- ❌ Licença Elastic divergiu do open source (BSL)

### OpenTelemetry + Serilog + Prometheus/Grafana + Seq ✅ **ESCOLHIDA**

**Por que OpenTelemetry?**
- Standard CNCF, suportado nativamente pelo .NET 10
- Vendor-neutral: pode exportar para DataDog, Jaeger, Tempo, New Relic sem mudar o código
- Uma instrumentação, múltiplos destinos

**Por que Serilog?**
- Logging estruturado (JSON) nativo
- Sinks configuráveis: Console, Seq, Elasticsearch, Application Insights
- Correlation ID automático com middleware
- Amplamente adotado no ecossistema .NET

**Por que Seq?**
- UI amigável para explorar logs estruturados em desenvolvimento
- Gratuito para uso individual
- Troca transparente por ELK/Loki em produção

**Por que Prometheus + Grafana?**
- Padrão de mercado para métricas em containers/Kubernetes
- Pull model (Prometheus scrape /metrics) — simples e confiável
- Grafana: dashboards prontos para ASP.NET Core, RabbitMQ, Redis

---

## Trade-off Decisório

| Trade-off | Impacto | Por que foi aceito |
|-----------|---------|--------------------|
| **Mais containers** (Seq, Prometheus, Grafana) no Docker Compose | Médio | São containers leves e de uso exclusivo para observabilidade. A alternativa — sem observabilidade — é inaceitável para um sistema financeiro. |
| **ELK seria mais poderoso** para log analytics | Médio/Futuro | Elasticsearch consome 2GB+ RAM só para rodar. Para um ambiente de desafio, o custo de memória e operação do ELK não justifica. Serilog → Seq entrega 90% do valor com 10% da complexidade. |
| **Vendor lock-in em DataDog/New Relic** eliminado | Positivo | OpenTelemetry + OTLP garante que a mesma instrumentação exporta para qualquer destino. Dados financeiros saindo para SaaS externo cria risco de compliance — stack própria mitiga isso. |
| **Curva de aprendizado** do Grafana + PromQL | Baixo | Dashboards prontos (ID 10427 para ASP.NET Core, ID 4279 para RabbitMQ) eliminam a curva inicial. Time aprende PromQL progressivamente. |
| **Traces distribuídos incompletos** (sem Jaeger no Docker Compose atual) | Baixo | OpenTelemetry já instrumenta o código. Adicionar exportador Jaeger é configuração, não código. Incluído no roadmap Fase 2. |

**O trade-off que definiu a escolha:** vendor lock-in em plataformas comerciais (DataDog, New Relic) foi o fator eliminatório principal — tanto por custo quanto por compliance de dados financeiros. OpenTelemetry + stack open-source entrega vendor-neutrality completa com zero custo de licença.

---

## Instrumentação Automática (Built-in .NET 10)

```csharp
// Com AddOpenTelemetry(), são instrumentados automaticamente:
// - Todas as requisições HTTP (entrada e saída)
// - Todas as queries EF Core (com duração e SQL)
// - Todas as mensagens MassTransit
// - Health checks
// Zero código adicional nas classes de negócio
```

---

## Dashboards Grafana Provisionados

| Dashboard | Métricas |
|-----------|---------|
| **SLA Overview** | Req/s, P50/P99 latência, taxa de erro |
| **Business Metrics** | Lançamentos criados/cancelados, valor total |
| **Infrastructure** | CPU, memória, conexões DB, queue depth |
| **Cache Performance** | Redis hit/miss ratio, latência cache |

---

## Consequências

**Positivas:**
- Vendor-neutral: fácil migração para qualquer plataforma cloud
- Zero custo de licença para ambiente on-premise
- Instrumentação automática sem poluir código de negócio
- Logs estruturados facilitam busca e correlação

**Negativas:**
- Mais containers no docker-compose (Seq, Prometheus, Grafana)
- Curva de aprendizado inicial com OpenTelemetry e Grafana
- Jaeger (traces distribuídos) não incluído nesta fase — previsto no roadmap Fase 2

---

## Histórico de Revisões

| Versão | Data | Autor | Descrição |
|--------|------|-------|-----------|
| 1.0 | Junho 2026 | Arquiteto de Soluções | Decisão inicial — MVP |
| 1.1 | Junho 2026 | Arquiteto de Soluções | Adicionada seção de trade-offs; Jaeger previsto no roadmap (Fase 2) |
| 1.2 | Junho 2026 | Arquiteto de Soluções | Esclarecido que Jaeger não foi implementado nesta fase; OTLP exporter presente no código |
- Em produção cloud, pode ser substituído por Azure Monitor (mais simples de operar)