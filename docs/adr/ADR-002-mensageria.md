# ADR-002 — RabbitMQ + MassTransit para Mensageria

| Campo | Valor |
|-------|-------|
| **ID** | ADR-002 |
| **Título** | Usar RabbitMQ com MassTransit como solução de mensageria |
| **Status** | ✅ Aprovado |
| **Data** | Junho 2026 |

---

## Contexto

Com a decisão de usar microsserviços (ADR-001), é necessário um mecanismo de comunicação assíncrona confiável entre os serviços de Lançamentos (publisher) e Consolidado (consumer). Os requisitos são:

- Mensagens duráveis (persistidas em disco) — sem perda mesmo com restart
- Suporte a múltiplas réplicas consumindo a mesma fila (competing consumers)
- Dead Letter Queue para mensagens com falha
- Bom suporte ao ecossistema .NET
- Operação em ambiente local (Docker) sem dependência de cloud

---

## Decisão

**RabbitMQ 3.12** como message broker, com **MassTransit 9** como abstração de alto nível no .NET.

---

## Alternativas Consideradas

### Apache Kafka ❌
- ✅ Melhor para event sourcing, replay de eventos, alto throughput (milhões/s)
- ❌ Overhead operacional significativo (ZooKeeper/KRaft, partitions, consumer groups)
- ❌ Complexidade desnecessária para ~50 req/s
- ❌ Não é pub-sub tradicional; curva de aprendizado maior

### Azure Service Bus ❌
- ✅ Totalmente gerenciado, integração nativa Azure
- ❌ Vendor lock-in (requer Azure, não roda local sem emulador)
- ❌ Custo de licença (Premium ~$670/mês para volume maior)
- Válido como evolução futura em ambiente Azure (MassTransit suporta troca transparente)

### RabbitMQ direto (sem MassTransit) ❌
- ✅ Menos overhead de abstração
- ❌ Tightly coupled ao RabbitMQ (dificulta troca futura para Azure Service Bus)
- ❌ Retry, error handling, DLQ precisam ser implementados manualmente

### RabbitMQ + MassTransit ✅ **ESCOLHIDA**
- ✅ Abstração que permite trocar o broker (RabbitMQ → Azure Service Bus) apenas por configuração
- ✅ Retry, circuit breaker, DLQ, saga — built-in
- ✅ Suporte nativo a competing consumers (múltiplas réplicas)
- ✅ Fácil setup local com Docker
- ✅ Ecossistema .NET maduro, amplamente adotado

---

## Trade-off Decisório

| Trade-off | Impacto | Por que foi aceito |
|-----------|---------|--------------------|
| **RabbitMQ = mais um container** para operar | Médio | Já necessário pela arquitetura de microsserviços (ADR-001). O custo operacional é absorvido pela escolha arquitetural prévia. |
| **Kafka seria mais escalável** para volumes futuros | Baixo/Futuro | Para ~50 req/s, Kafka seria over-engineering. RabbitMQ suporta dezenas de milhares de msg/s. A troca seria via MassTransit sem mudar o código de negócio. |
| **MassTransit adiciona overhead** de abstração | Baixo | O overhead (~1ms por mensagem) é desprezível frente aos benefícios: retry automático, DLQ, suporte a competing consumers e portabilidade de broker. |
| **Vendor lock-in em RabbitMQ** (protocolo AMQP) | Baixo | Mitigado integralmente pelo MassTransit: trocar para Azure Service Bus ou Amazon SQS requer apenas mudança de configuração, sem alterar handlers. |
| **At-least-once delivery** (mensagem pode chegar 2x) | Médio | Mitigado pelo Idempotent Consumer (`EventosProcessados`): duplicatas são detectadas e ignoradas antes de processar. |

**O trade-off que definiu a escolha:** Kafka resolveria problemas que este sistema não tem (replay, event sourcing, throughput de milhões/s). O custo operacional de Kafka supera qualquer benefício para o volume atual. MassTransit + RabbitMQ entrega o necessário com esforço operacional proporcional.

---

## Configuração Adotada

```
Exchange: fluxo-caixa.lancamentos (topic)
Routing Keys:
  - lancamento.criado
  - lancamento.cancelado

Queue: consolidado-lancamentos-queue (durable: true)
DLQ:   consolidado-lancamentos-queue_error

Retry Policy: 3 tentativas com intervalo exponencial (1s, 2s, 4s)
```

---

## Consequências

**Positivas:**
- Desacoplamento total entre Lançamentos e Consolidado
- Mensagens duráveis garantem zero perda em caso de restart do Consolidado
- MassTransit facilita evolução futura para Azure Service Bus sem mudança de código

**Negativas:**
- RabbitMQ é mais uma peça de infraestrutura para gerenciar
- Consistência eventual (consolidado atualizado em ~1-5s após lançamento)

---

## Histórico de Revisões

| Versão | Data | Autor | Descrição |
|--------|------|-------|-----------|
| 1.0 | Junho 2026 | Arquiteto de Soluções | Decisão inicial — MVP |
| 1.1 | Junho 2026 | Arquiteto de Soluções | Adicionada seção de trade-offs explícita |