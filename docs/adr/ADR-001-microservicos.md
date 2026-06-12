# ADR-001 — Adotar Arquitetura de Microsserviços

| Campo | Valor |
|-------|-------|
| **ID** | ADR-001 |
| **Título** | Adotar Arquitetura de Microsserviços com Comunicação Assíncrona |
| **Status** | ✅ Aprovado |
| **Data** | Junho 2026 |
| **Autores** | Arquiteto de Soluções |
| **Revisores** | Tech Lead, CTO |

---

## Contexto

O sistema precisa implementar dois serviços: controle de lançamentos e consolidado diário. O requisito não funcional mais crítico determina que:

> *"O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário cair."*

Este requisito é determinístico: **os dois serviços devem ser fisicamente separados e operacionalmente independentes**.

---

## Decisão

Adotar **Arquitetura de Microsserviços** com:
- Dois microsserviços separados (Lançamentos e Consolidado)
- Comunicação assíncrona via Message Broker (RabbitMQ)
- Banco de dados por serviço (Database per Service pattern)

---

## Alternativas Consideradas

### Opção A: Monolito com módulos separados ❌
- Simples de implementar
- ❌ **Falha no RNF**: se o processo cair, ambas as funcionalidades ficam inativas
- ❌ Deploy conjunto, escalonamento conjunto

### Opção B: Monolito Modular com comunicação síncrona ❌
- Melhor organização que monolito puro
- ❌ **Ainda falha no RNF**: comunicação síncrona (HTTP) entre módulos = dependência de disponibilidade
- ❌ Se Consolidado tiver timeout, afeta Lançamentos

### Opção C: SOA com ESB ❌
- Comunicação desacoplada
- ❌ Overhead de ESB (Enterprise Service Bus), complexidade desnecessária para este porte
- ❌ Centraliza lógica de roteamento, cria SPOF

### Opção D: Microsserviços com Event-Driven ✅ **ESCOLHIDA**
- ✅ **Atende RNF-01**: Lançamentos não chama Consolidado; apenas publica evento
- ✅ Escalamento independente (Consolidado é read-heavy, pode escalar mais)
- ✅ Fault isolation: falha do Consolidado não impacta Lançamentos
- ⚠️ Consistência eventual (aceita pelo negócio — consolidado atualizado em ≤ 5s)
- ⚠️ Maior complexidade operacional (mitigada com Docker Compose + documentação)

---

## Trade-off Decisório

| Trade-off | Impacto | Por que foi aceito |
|-----------|---------|--------------------|
| **Consistência eventual** (consolidado ~1-5s atrasado) | Médio | O negócio aceita: o consolidado é relatório de fechamento, não precisa ser instantâneo. A janela de inconsistência é imperceptível operacionalmente. |
| **Complexidade operacional** (2 serviços, RabbitMQ, 2 bancos) | Alto | Mitigado com Docker Compose + `MigrateAsync()` no startup. A independência operacional exigida pelo RNF-01 não tem alternativa mais simples. |
| **Mais superfície de falha** (broker pode cair) | Médio | Mitigado pelo Transactional Outbox: eventos ficam no banco até serem entregues. Broker indisponível não perde lançamentos. |
| **Debugging mais complexo** (trace distribuído) | Baixo | Mitigado com OpenTelemetry + CorrelationId propagado entre serviços via headers. |

**O trade-off que definiu a escolha:** o RNF-01 é binário — ou os serviços são fisicamente separados, ou não. Qualquer arquitetura que mantivesse os dois em processo compartilhado falhava no requisito, independente do benefício de simplicidade.

---

## Consequências

**Positivas:**
- Serviços independentes e implantáveis separadamente
- Escalonamento horizontal por serviço
- Fault isolation completo
- Tecnologias podem evoluir independentemente

**Negativas / Trade-offs:**
- Consistência eventual no consolidado (dados com ~1-5s de delay)
- Infraestrutura adicional: RabbitMQ, dois bancos de dados

---

## Status de Implementação

| Item | Status | Observação |
|------|--------|------------|
| Microsserviço Lançamentos | ✅ Implementado | API REST + Domain + Application + Infrastructure |
| Microsserviço Consolidado | ✅ Implementado | Consumer + Cache-Aside + Read-model |
| Comunicação assíncrona via RabbitMQ | ✅ Implementado | MassTransit + eventos de integração |
| Transactional Outbox | ✅ Implementado | `OutboxMessage` + `OutboxProcessor` BackgroundService |
| Idempotency Key (escrita) | ✅ Implementado | Índice único filtrado na tabela Lançamentos |
| Idempotent Consumer (leitura) | ✅ Implementado | `EventosProcessados` ledger no Consolidado DB |
| Auditoria persistida | ✅ Implementado | `AuditLog` + `AuditBehavior` (MediatR pipeline) |
| Testes k6 baseline | ✅ Implementado | `tests/k6/lancamentos-baseline.js` e `consolidado-baseline.js` |
| Kubernetes + HPA | 🔜 Fase 2 | Escalonamento automático do Consolidado |
| OAuth2/JWT (Keycloak/Azure AD) | 🔜 Fase 2 | Substitui API Key em produção |

---

## Histórico de Revisões

| Versão | Data | Autor | Descrição |
|--------|------|-------|-----------|
| 1.0 | Junho 2026 | Arquiteto de Soluções | Decisão inicial — MVP |
| 1.1 | Junho 2026 | Arquiteto de Soluções | Adicionada seção de trade-offs e atualizado roadmap com auditoria e testes k6 |
- Maior complexidade de desenvolvimento e operação