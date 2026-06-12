# 💰 Fluxo de Caixa Diário

> **Desafio Técnico — Arquiteto de Soluções**
> Solução para controle de fluxo de caixa diário com lançamentos (débitos e créditos) e relatório de saldo consolidado diário.

---

> ## ⚠️ Aviso de Segurança — Credenciais no Repositório
>
> **Este repositório contém intencionalmente credenciais, senhas e configurações de ambiente em texto claro** (ex.: strings de conexão com SQL Server, senha do RabbitMQ, API Key placeholder, senha do Redis). Isso foi feito **exclusivamente para facilitar a avaliação e execução local deste desafio técnico**, eliminando a necessidade de configuração manual do ambiente por parte do avaliador.
>
> **Em um projeto real — de desenvolvimento ou produção — isso jamais deve acontecer.** As boas práticas adotadas no mercado são:
>
> | Prática | Ferramentas / Como aplicar |
> |---------|---------------------------|
> | **Nunca versionar segredos** | Adicionar `appsettings.*.json`, `.env` e arquivos de certificado ao `.gitignore`; usar apenas placeholders como `CHANGE_ME` nos arquivos versionados |
> | **Variáveis de ambiente** | Sobrescrever configurações sensíveis em runtime via variáveis de ambiente (`ApiKey__Key`, `ConnectionStrings__LancamentosDb`, etc.) — o ASP.NET Core já suporta isso nativamente |
> | **Gerenciadores de segredos locais** | `dotnet user-secrets` para desenvolvimento local — os valores ficam fora da árvore do projeto, em `%APPDATA%\Microsoft\UserSecrets` |
> | **Cofres de segredos em nuvem** | [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/), [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/) ou [HashiCorp Vault](https://www.vaultproject.io/) para ambientes de staging e produção |
> | **CI/CD com segredos protegidos** | GitHub Actions Secrets, Azure DevOps Variable Groups ou equivalente — os valores são injetados na pipeline sem aparecer nos logs |
> | **Rotação periódica de segredos** | Automatizada via Key Vault + Managed Identity, sem downtime e sem alterar o código |
> | **Auditoria e detecção** | Ferramentas como [git-secrets](https://github.com/awslabs/git-secrets), [truffleHog](https://github.com/trufflesecurity/trufflehog) ou [GitGuardian](https://www.gitguardian.com/) para detectar credenciais acidentalmente commitadas |
>
> A decisão de arquitetura relacionada à autenticação está documentada em [ADR-006](docs/adr/ADR-006-autenticacao-api-key.md).

---

## 📋 Sumário

- [Visão Geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Estrutura do Repositório](#estrutura-do-repositório)
- [Documentação de Arquitetura](#documentação-de-arquitetura)
- [Pré-requisitos](#pré-requisitos)
- [Como Executar Localmente](#como-executar-localmente)
- [Segurança](#segurança)
- [APIs Disponíveis](#apis-disponíveis)
- [Testes](#testes)
- [Observabilidade](#observabilidade)
- [Auditoria](#auditoria)
- [Decisões Técnicas](#decisões-técnicas)
- [Roadmap e Evoluções Futuras](#roadmap-e-evoluções-futuras)

---

## Visão Geral

O sistema **Fluxo de Caixa Diário** permite que um comerciante:

1. **Registre lançamentos** (débitos e créditos) de forma confiável e auditável
2. **Consulte o saldo consolidado diário**, com alta disponibilidade mesmo em picos de 50 req/s

A solução é composta por **dois microsserviços independentes**, comunicando-se de forma assíncrona via mensageria, garantindo que o serviço de lançamentos **nunca fique indisponível** mesmo se o serviço de consolidado estiver fora do ar.

### Requisitos de Negócio Atendidos

| Requisito | Status | Solução |
|-----------|--------|---------|
| Controle de lançamentos (débito/crédito) | ✅ | `Lancamentos.API` — POST/GET/DELETE |
| Relatório de saldo consolidado diário | ✅ | `Consolidado.API` — GET por data |
| Lançamentos independente do Consolidado | ✅ | Comunicação assíncrona via RabbitMQ |
| 50 req/s com máx. 5% de perda | ✅ | Redis cache + Rate Limiting + Polly |
| Garantia de entrega de eventos | ✅ | Transactional Outbox Pattern |
| Processamento único (sem duplicatas) | ✅ | Idempotency Key + Idempotent Consumer |
| Segurança básica nos endpoints | ✅ | API Key Authentication (header `X-Api-Key`) |

---

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Gateway (Nginx)                      │
└──────────────┬──────────────────────────────┬───────────────────┘
               │                              │
   ┌───────────▼──────────┐      ┌────────────▼────────────┐
   │   Lancamentos API    │      │    Consolidado API       │
   │   (ASP.NET Core 10)  │      │    (ASP.NET Core 10)     │
   └───────────┬──────────┘      └────────────┬────────────┘
               │ persiste                      │ lê
   ┌───────────▼──────────┐      ┌────────────▼────────────┐
   │   SQL Server         │      │  Redis Cache + SQL Server│
   │   (Lancamentos DB)   │      │  (Consolidado DB)        │
   └───────────┬──────────┘      └────────────▲────────────┘
               │ publica                       │ consome
               └────────────┐  ┌──────────────┘
                    ┌────────▼──▼────────┐
                    │     RabbitMQ       │
                    │  (Message Broker)  │
                    └────────────────────┘
```

**Padrões Utilizados:**
- 🏛️ **Clean Architecture** por serviço (Domain → Application → Infrastructure → API)
- 📐 **DDD** (Aggregates, Value Objects, Domain Events)
- ⚡ **CQRS** via MediatR (Commands separados de Queries)
- 📨 **Event-Driven Architecture** via MassTransit + RabbitMQ
- 🔄 **Cache-Aside Pattern** com Redis no serviço de consolidado
- 📦 **Transactional Outbox Pattern** — evento e lançamento persistidos na mesma transação; background processor publica no broker
- 🔑 **Idempotency Key** — chave única por comando + índice filtrado no banco; consumidores verificam ledger de eventos processados antes de aplicar projeções

---

## Estrutura do Repositório

```
FluxoCaixaDiario/
├── README.md
├── docker-compose.yml
├── docker-compose.override.yml
├── FluxoCaixaDiario.sln
│
├── docs/
│   ├── 01-arquitetura-enterprise.md       # Visão Enterprise (TOGAF-aligned)
│   ├── 02-arquitetura-solucao.md          # ⭐ Arquitetura de Solução (C4 + ADRs)
│   ├── 03-arquitetura-software.md         # Arquitetura de Software (Clean Arch)
│   ├── 04-apresentacao-executiva.md       # Deck de apresentação
│   └── adr/
│       ├── ADR-001-microservicos.md
│       ├── ADR-002-mensageria.md
│       ├── ADR-003-persistencia.md
│       ├── ADR-004-cache.md
│       ├── ADR-005-observabilidade.md
│       └── ADR-006-autenticacao-api-key.md
│
├── src/
│   ├── Shared/
│   │   └── FluxoCaixaDiario.SharedKernel/   # AuditLog, OutboxMessage, Value Objects
│   │
│   ├── Lancamentos/
│   │   ├── FluxoCaixaDiario.Lancamentos.Domain/
│   │   ├── FluxoCaixaDiario.Lancamentos.Application/   # AuditBehavior, Commands, Handlers
│   │   ├── FluxoCaixaDiario.Lancamentos.Infrastructure/ # AuditRepository, OutboxProcessor
│   │   └── FluxoCaixaDiario.Lancamentos.API/
│   │
│   └── Consolidado/
│       ├── FluxoCaixaDiario.Consolidado.Domain/
│       ├── FluxoCaixaDiario.Consolidado.Application/   # Consumers (EventoProcessado)
│       ├── FluxoCaixaDiario.Consolidado.Infrastructure/ # AuditRepository, Cache
│       └── FluxoCaixaDiario.Consolidado.API/
│
└── tests/
    ├── FluxoCaixaDiario.Lancamentos.UnitTests/
    ├── FluxoCaixaDiario.Consolidado.UnitTests/
    └── k6/                                   # Testes de carga baseline (aceite)
        ├── lancamentos-baseline.js
        ├── consolidado-baseline.js
        └── README.md
```

---

## Documentação de Arquitetura

| Documento | Audiência | Descrição |
|-----------|-----------|-----------|
| [📊 Arquitetura Enterprise](docs/01-arquitetura-enterprise.md) | CTO, Diretores, EA Board | Capacidades de negócio, domínios funcionais, mapa de aplicações |
| [🏗️ Arquitetura de Solução](docs/02-arquitetura-solucao.md) | Arquitetos, Tech Leads | Visão completa com diagramas C4, ADRs, custos, riscos |
| [💻 Arquitetura de Software](docs/03-arquitetura-software.md) | Desenvolvedores | Clean Arch, DDD, padrões de código, estratégia de testes |
| [📑 Apresentação Executiva](docs/04-apresentacao-executiva.md) | Stakeholders | Deck de apresentação da solução |

---

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) 4.x+
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)

> **Opcional:** Visual Studio 2022+ / Visual Studio 2026+ ou Rider para desenvolvimento

---

## Como Executar Localmente

### 1. Clonar o repositório

```bash
git clone https://github.com/afborges/FluxoCaixaDiario.git
cd FluxoCaixaDiario
```

### 2. Subir a infraestrutura completa com Docker Compose

```bash
docker-compose up -d
```

Isso irá subir:

| Serviço | URL Local | Credenciais |
|---------|-----------|-------------|
| Lancamentos API | http://localhost:5010 | API Key (ver [Segurança](#segurança)) |
| Consolidado API | http://localhost:5011 | API Key (ver [Segurança](#segurança)) |
| API Gateway (Nginx) | http://localhost:8080 | — |
| RabbitMQ Management | http://localhost:15672 | guest/guest |
| SQL Server | localhost:1433 | sa/FluxoCaixa@2024 |
| Redis | localhost:6379 | — |
| Seq (Logs) | http://localhost:5341 | — |
| Grafana | http://localhost:3000 | admin/admin |
| Prometheus | http://localhost:9090 | — |

### 3. Acessar a documentação Swagger

- Lançamentos: http://localhost:5010/swagger
- Consolidado: http://localhost:5011/swagger

> 🔐 No Swagger UI, clique em **Authorize** (canto superior direito) e informe a chave configurada em `appsettings.json` → `ApiKey:Key` (padrão: `TROQUE-ESTA-CHAVE-EM-PRODUCAO`). Sem isso, todos os endpoints retornarão `401 Unauthorized`.

### 4. Executar apenas via .NET CLI (sem Docker)

```bash
# Pré-requisito: .NET 10 SDK instalado + SQL Server, RabbitMQ e Redis disponíveis
# Configure as strings de conexão nos appsettings.Development.json

# (Opcional) Definir a API Key via variável de ambiente
$env:ApiKey__Key = "minha-chave-secreta"   # PowerShell
# export ApiKey__Key="minha-chave-secreta" # bash/zsh

# Terminal 1 — Serviço de Lançamentos
cd src/Lancamentos/FluxoCaixaDiario.Lancamentos.API
dotnet run

# Terminal 2 — Serviço de Consolidado
cd src/Consolidado/FluxoCaixaDiario.Consolidado.API
dotnet run
```

---

## Segurança

Ambos os serviços protegem seus endpoints com **API Key Authentication** via header HTTP. Sem o header correto, qualquer chamada aos endpoints de negócio retorna `401 Unauthorized`.

### Como autenticar

Incluir o header `X-Api-Key` em **toda** requisição:

```bash
curl -X POST http://localhost:5010/api/lancamentos \
  -H "X-Api-Key: TROQUE-ESTA-CHAVE-EM-PRODUCAO" \
  -H "Content-Type: application/json" \
  -d '{...}'
```

### Configuração da chave

| Ambiente | Como configurar |
|----------|-----------------|
| **Desenvolvimento** | `appsettings.json` → `ApiKey:Key` (já preenchido com placeholder) |
| **PowerShell / Windows** | `$env:ApiKey__Key = "minha-chave-secreta"` |
| **bash / Linux / macOS** | `export ApiKey__Key="minha-chave-secreta"` |
| **Docker Compose** | variável de ambiente no serviço: `ApiKey__Key=minha-chave-secreta` |
| **Produção** | Secret Manager (Azure Key Vault, AWS Secrets Manager, etc.) |

> 📋 **Nota de avaliação:** conforme o aviso no início deste documento, a chave real está intencionalmente versionada neste repositório para facilitar a execução e a avaliação do desafio. Em um projeto real isso jamais deve ocorrer — segredos devem ser gerenciados via variáveis de ambiente, `dotnet user-secrets` ou cofres de segredos em nuvem (Azure Key Vault, AWS Secrets Manager etc.).

### Referencia rápida de comportamento

| Situação | Resposta |
|-----------|----------|
| Header `X-Api-Key` ausente | `401 Unauthorized` |
| Header `X-Api-Key` com valor errado | `401 Unauthorized` |
| Chave válida | Request processado normalmente |
| Endpoints `/health`, `/metrics`, `/swagger` | Sem autenticação (sempre acessíveis) |

### Swagger UI

1. Acesse http://localhost:5010/swagger (ou `:5011` para Consolidado)
2. Clique em **Authorize** 🔒 (canto superior direito)
3. Informe o valor de `ApiKey:Key` do `appsettings.json`
4. Clique **Authorize** e depois **Close**
5. Todos os endpoints já enviarão o header automaticamente

| Aspecto | Detalhe |
|---------|---------|
| Header | `X-Api-Key` |
| Configuração | `appsettings.json` → seção `ApiKey:Key` (substituível por variável de ambiente `ApiKey__Key`) |
| Endpoints isentos | `/health`, `/metrics`, `/swagger` |
| Resposta sem chave | `401 Unauthorized` |
| Rate Limiting | 100 req/s por janela fixa; excesso retorna `429 Too Many Requests` |
| Response headers | `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `X-XSS-Protection: 1; mode=block` |
| Transport | HTTPS redirection habilitada (em produção use certificado TLS válido) |

> **Roadmap de segurança:** OAuth2/JWT com Keycloak ou Azure AD B2C para autenticação delegada e autorização baseada em escopos. Decisão documentada em [ADR-006](docs/adr/ADR-006-autenticacao-api-key.md).

> ⚠️ **Disclaimer — Swagger em produção:** O Swagger UI só está ativo no ambiente `Development` (`IsDevelopment()`). Em produção, o endpoint `/swagger` está desabilitado. Para um cenário real, recomenda-se também proteger o Swagger com autenticação ou restringi-lo por IP/VPN. Esta avaliação não implementa essa restrição adicional.

---

## APIs Disponíveis

### Serviço de Lançamentos (`/api/lancamentos`)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/lancamentos` | Registrar novo lançamento |
| `GET` | `/api/lancamentos` | Listar lançamentos (filtros opcionais) |
| `GET` | `/api/lancamentos/{id}` | Obter lançamento por ID |
| `DELETE` | `/api/lancamentos/{id}` | Cancelar lançamento |
| `GET` | `/health` | Health check do serviço |

**Exemplo — Criar Lançamento:**
```bash
curl -X POST http://localhost:5010/api/lancamentos \
  -H "X-Api-Key: TROQUE-ESTA-CHAVE-EM-PRODUCAO" \
  -H "Content-Type: application/json" \
  -d '{
    "tipo": "Credito",
    "valor": 1500.00,
    "descricao": "Venda à vista",
    "data": "2026-06-15"
  }'
```

### Serviço de Consolidado (`/api/consolidado`)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/api/consolidado/{data}` | Saldo consolidado do dia (yyyy-MM-dd) |
| `GET` | `/api/consolidado` | Histórico consolidado (range de datas) |
| `GET` | `/health` | Health check do serviço |

**Exemplo — Consultar Consolidado:**
```bash
curl http://localhost:5011/api/consolidado/2026-06-15 \
  -H "X-Api-Key: TROQUE-ESTA-CHAVE-EM-PRODUCAO"
```

**Resposta:**
```json
{
  "data": "2026-06-15",
  "totalCreditos": 2500.00,
  "totalDebitos": 800.00,
  "saldoFinal": 1700.00,
  "quantidadeLancamentos": 5,
  "ultimaAtualizacao": "2026-06-15T18:30:00Z"
}
```

---

## Testes

### Testes Unitários

```bash
# Executar todos os testes
dotnet test FluxoCaixaDiario.sln

# Com cobertura de código
dotnet test FluxoCaixaDiario.sln --collect:"XPlat Code Coverage"

# Testes específicos por projeto
dotnet test tests/FluxoCaixaDiario.Lancamentos.UnitTests/
dotnet test tests/FluxoCaixaDiario.Consolidado.UnitTests/
```

**Status atual:** 43 testes unitários passando (domain + application layers de ambos os serviços).

**Distribuição por serviço e camada:**

| Projeto de Testes | Camada | Testes |
|---|---|--:|
| `FluxoCaixaDiario.Lancamentos.UnitTests` | Domain | 24 |
| `FluxoCaixaDiario.Lancamentos.UnitTests` | Application | 8 |
| `FluxoCaixaDiario.Consolidado.UnitTests` | Domain | 8 |
| `FluxoCaixaDiario.Consolidado.UnitTests` | Application | 3 |
| **Total** | | **43** |

**Verificar cobertura por camada (Coverlet + ReportGenerator):**

```bash
# Gerar cobertura com filtro por assembly
dotnet test FluxoCaixaDiario.sln \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Instalar ReportGenerator (uma vez)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Relatório HTML com breakdown por assembly/namespace
reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:"Html;TextSummary" \
  -classfilters:"-*DbContext*;-*Migrations*"

# Abrir relatório
start ./coverage/report/index.html
```

> **Interpretando o relatório:** filtre por `FluxoCaixaDiario.Lancamentos.Domain`, `FluxoCaixaDiario.Lancamentos.Application`, `FluxoCaixaDiario.Consolidado.Domain` e `FluxoCaixaDiario.Consolidado.Application` para ver cobertura isolada por camada. Camadas Infrastructure e API não possuem testes unitários nesta fase (cobertura via testes de integração planejados no roadmap).

---

### Testes de Carga (k6)

> **Escopo dos testes incluídos:** Os scripts k6 em `tests/k6/` são **testes de aceitação baseline** — seu único objetivo é confirmar que o sistema atende ao **requisito mínimo funcional** (50 req/s no Consolidado, p95 < 200ms nos Lançamentos) em condições normais de pico.
>
> **O que eles NÃO fazem:** determinar limiares de degradação, pontos de ruptura, comportamento sob carga sustentada ou spikes abruptos. Esses cenários (**stress**, **soak** e **spike tests**) são a próxima evolução planejada — consulte o roadmap em `tests/k6/README.md`.

**Pré-requisito:** [k6](https://k6.io/docs/getting-started/installation/) instalado + sistema rodando via `docker-compose up -d`

```bash
# Teste baseline — Lançamentos (valida p95 < 200ms com 20 VUs ramping)
k6 run tests/k6/lancamentos-baseline.js

# Teste baseline — Consolidado (valida 50 req/s constante com cache ativo)
k6 run tests/k6/consolidado-baseline.js

# Com saída JSON para análise posterior
k6 run --out json=tests/k6/results/lancamentos.json tests/k6/lancamentos-baseline.js
```

**Thresholds de aceitação (baseline):**

| Métrica | Lançamentos | Consolidado |
|---------|-------------|-------------|
| P95 latência | `< 200ms` | `< 100ms` |
| Taxa de erro | `< 1%` | `< 1%` |
| Throughput mínimo | 20 VUs ramping | 50 req/s constant |

**Resultados esperados ao passar:**
- ✅ O sistema é capaz de processar o volume normal de pico
- ✅ O cache Redis está ativo e reduzindo latência do Consolidado
- ✅ Nenhum erro de validação ou timeout em operação normal

**O que ainda falta (roadmap):**
- ⏳ Stress test — descobrir o ponto de degradação
- ⏳ Soak test — estabilidade sob carga sustentada por horas
- ⏳ Spike test — recuperação após burst repentino

> Para detalhes de execução, interpretação dos resultados e roadmap completo de testes de performance, consulte [`tests/k6/README.md`](tests/k6/README.md).

---

## Observabilidade

Após subir com `docker-compose up -d`, o seguinte stack de observabilidade está disponível:

| Ferramenta | URL | Credenciais | Finalidade |
|-----------|-----|-------------|------------|
| **Seq** | http://localhost:5341 | sem auth | Logs estruturados (JSON) de todos os serviços |
| **Grafana** | http://localhost:3000 | admin / admin | Dashboards de métricas em tempo real |
| **Prometheus** | http://localhost:9090 | sem auth | Coleta de métricas |
| **RabbitMQ Mgmt** | http://localhost:15672 | guest / guest | Filas, DLQ, taxa de consumo |

> ⚠️ **Jaeger não está disponível neste ambiente.** O rastreamento distribuído via Jaeger foi planejado na arquitetura (ADR-005) mas não foi implementado no prazo deste desafio por questão de tempo. Está previsto no roadmap. Correlação de traces entre serviços é feita hoje pelo campo `CorrelationId` nos logs do Seq.

---

### 📋 Logs Estruturados — Seq

Acesse http://localhost:5341. Todos os logs são emitidos em JSON via **Serilog** e indexados automaticamente.

**Queries úteis no Seq:**
```
# Acompanhar todos os eventos de um request específico (correlation-id)
CorrelationId = "abc-123"

# Erros nas últimas 1 hora
@Level = 'Error' and @Timestamp > Now() - 1h

# Lançamentos criados acima de R$ 1.000
@MessageTemplate like '%Lançamento criado%' and Valor > 1000

# Rastrear um lançamento pelo ID
LancamentoId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"

# Ver registros de auditoria logados (AuditBehavior)
@MessageTemplate like '%[Audit]%'

# Mensagens processadas pelo OutboxProcessor
@MessageTemplate like '%Outbox%'
```

> 💡 **Dica:** Use o campo `CorrelationId` para cruzar logs do Lançamentos API com os do Consolidado API dentro de um mesmo fluxo.

---

### 📈 Métricas em Tempo Real — Grafana + Prometheus

**Acessar o Grafana:**
1. Abra http://localhost:3000
2. Login: `admin` / `admin`
3. Vá em **Dashboards → Browse** para ver os dashboards pré-provisionados

**Métricas-chave para acompanhar:**

| Métrica Prometheus | Descrição | Alerta sugerido |
|-------------------|-----------|----------------|
| `http_server_request_duration_seconds` | Latência das requisições HTTP | P99 > 500ms |
| `lancamentos_criados_total` | Total de lançamentos criados (por `tipo`) | — |
| `lancamentos_cancelados_total` | Total de lançamentos cancelados | — |
| `consolidado_cache_hits_total` | Total de leituras atendidas pelo Redis | < 80% hit ratio |
| `consolidado_cache_misses_total` | Total de miss no cache Redis | — |
| `consolidacoes_processadas_total` | Eventos processados pelo Consolidado (por `tipo`) | — |

**Endpoints de scraping do Prometheus:**
- Lançamentos: `http://localhost:5010/metrics`
- Consolidado: `http://localhost:5011/metrics`
| `rabbitmq_queue_messages` | Profundidade da fila RabbitMQ | > 100 msgs |
| `rabbitmq_queue_messages_dlq` | Mensagens na Dead Letter Queue | > 0 |

**Queries PromQL úteis:**

```promql
# P99 de latência do Consolidado nos últimos 5 minutos
histogram_quantile(0.99,
  sum(rate(http_server_request_duration_seconds_bucket{
    job="consolidado-api"
  }[5m])) by (le)
)

# Taxa de erros por serviço (últimos 5 min)
rate(http_server_requests_total{status=~"5.."}[5m])

# Throughput (req/s) atual do Consolidado
rate(http_server_requests_total{job="consolidado-api"}[1m])
```

> Para consultas ad-hoc no Prometheus: http://localhost:9090/graph

---

### 🔍 Rastreamento Distribuído — Jaeger (Roadmap)

> ⚠️ **Não implementado nesta versão.** A integração com Jaeger para rastreamento distribuído end-to-end foi prevista na arquitetura (ver [ADR-005](docs/adr/ADR-005-observabilidade.md)) mas não foi incluída no escopo de entrega deste desafio por limitação de tempo.
>
> **Alternativa atual:** use o campo `CorrelationId` nos logs do Seq para cruzar eventos entre os dois serviços dentro de um mesmo fluxo. Cada requisição gera automaticamente um `CorrelationId` que é propagado via `AuditBehavior`, `LancamentoCriadoConsumer` e `LancamentoCanceladoConsumer`.
>
> **Evolução planejada (Fase 2):** integrar o exportador OTLP (`AddOtlpExporter`) do OpenTelemetry com Jaeger ou Tempo (Grafana stack), adicionando spans por handler MediatR, consumer MassTransit e chamadas ao Redis. Isso permitirá rastrear o fluxo completo `API → RabbitMQ → Consumer → Redis → DB` em um único trace.

---

### 🐰 RabbitMQ Management

Acesse http://localhost:15672 (`guest`/`guest`) para monitorar:

| O que verificar | Onde encontrar | Sinal de alerta |
|----------------|---------------|----------------|
| Profundidade da fila principal | `Queues → consolidado-lancamentos-queue` | > 100 mensagens acumuladas |
| Mensagens na DLQ | `Queues → consolidado-lancamentos-queue_error` | Qualquer mensagem presente |
| Taxa de consumo | `Queues → Message rates` | Taxa de consumo < taxa de publicação |
| Conexões ativas | `Connections` | Ausência de conexão dos serviços |

> ⚠️ Mensagens na DLQ indicam falha no processamento pelo Consolidado mesmo após retries. Verifique os logs no Seq com `@Level = 'Error'` e o `CorrelationId` correspondente.

---

## Auditoria

O sistema registra automaticamente **toda operação de escrita** em uma tabela `AuditLogs` em cada banco de dados, de forma **transparente para o código de negócio**:

- No serviço **Lançamentos**: via `AuditBehavior` no pipeline MediatR, interceptando todos os Commands antes e após a execução do Handler.
- No serviço **Consolidado**: via interceptação nos consumers MassTransit, registrando o resultado de cada evento processado.

### O que é auditado

| Serviço | Operações auditadas |
|---------|--------------------|
| **Lançamentos** | `CriarLancamento`, `CancelarLancamento` |
| **Consolidado** | `AplicarLancamentoCriado`, `ReverterLancamentoCancelado` |

### Campos registrados por entrada de auditoria

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Servico` | string | `Lancamentos` ou `Consolidado` |
| `Operacao` | string | Nome do Command ou Consumer |
| `UsuarioOuChave` | string | Sufixo mascarado da API Key (ex: `****cHAvE`) |
| `CorrelationId` | string | Valor do header `X-Correlation-Id` |
| `DadosEntrada` | JSON | Payload do request (serializado) |
| `DadosSaida` | JSON | Payload da resposta (serializado) |
| `Sucesso` | bool | `true` em sucesso, `false` em falha |
| `MensagemErro` | string | Mensagem de erro quando `Sucesso = false` |
| `OcorridoEm` | datetime | Timestamp UTC da operação |
| `DuracaoMs` | long | Tempo total de execução em milissegundos |

### Como consultar os registros de auditoria

**Conexão ao SQL Server local (Docker):**

| Banco | Server | Database | User | Senha |
|-------|--------|----------|------|-------|
| Lançamentos | `localhost,1433` | `LancamentosDb` | `sa` | `FluxoCaixa@2024` |
| Consolidado | `localhost,1433` | `ConsolidadoDb` | `sa` | `FluxoCaixa@2024` |

> Use **Azure Data Studio**, **SSMS** ou `sqlcmd` para conectar.

**Queries de auditoria — exemplos práticos:**

```sql
-- Todas as operações das últimas 24h (mais recentes primeiro)
SELECT Operacao, UsuarioOuChave, CorrelationId, Sucesso, DuracaoMs, OcorridoEm
FROM AuditLogs
WHERE OcorridoEm >= DATEADD(HOUR, -24, GETUTCDATE())
ORDER BY OcorridoEm DESC;

-- Apenas falhas (investigar erros)
SELECT Operacao, UsuarioOuChave, CorrelationId, MensagemErro, OcorridoEm
FROM AuditLogs
WHERE Sucesso = 0
ORDER BY OcorridoEm DESC;

-- Rastrear todas as operações de um correlation-id (fluxo completo)
SELECT Servico, Operacao, Sucesso, DuracaoMs, OcorridoEm
FROM AuditLogs
WHERE CorrelationId = 'seu-correlation-id'
ORDER BY OcorridoEm;

-- Operações de um usuário específico (por sufixo da API Key)
SELECT Operacao, CorrelationId, Sucesso, DuracaoMs, OcorridoEm
FROM AuditLogs
WHERE UsuarioOuChave LIKE '%cHAvE'
ORDER BY OcorridoEm DESC;

-- Operações lentas (> 500ms)
SELECT Operacao, CorrelationId, DuracaoMs, OcorridoEm
FROM AuditLogs
WHERE DuracaoMs > 500
ORDER BY DuracaoMs DESC;

-- Volume por tipo de operação (últimas 24h)
SELECT Operacao, COUNT(*) AS Total,
       SUM(CASE WHEN Sucesso = 1 THEN 1 ELSE 0 END) AS Sucesso,
       SUM(CASE WHEN Sucesso = 0 THEN 1 ELSE 0 END) AS Falhas,
       AVG(DuracaoMs) AS MediaMs
FROM AuditLogs
WHERE OcorridoEm >= DATEADD(HOUR, -24, GETUTCDATE())
GROUP BY Operacao
ORDER BY Total DESC;
```

> 💡 **Dica de investigação:** Ao receber um ticket de suporte com um `CorrelationId`, execute a query de rastreamento **em ambos os bancos** (LancamentosDb e ConsolidadoDb) para ver o fluxo completo de ponta a ponta.

> **Nota de evolução:** Um endpoint REST `GET /api/auditoria` com filtros por operação, período e correlation-id está previsto para a Fase 2 do roadmap. Atualmente, o acesso é direto ao banco de dados.

---

## Decisões Técnicas

| Decisão | Escolha | Alternativa Considerada | Motivo |
|---------|---------|------------------------|--------|
| Arquitetura | Microsserviços | Monolito Modular | NFR: serviços devem ser independentes |
| Mensageria | RabbitMQ + MassTransit | Kafka, Azure Service Bus | Custo, maturidade, suporte .NET nativo |
| ORM | Entity Framework Core 10 | Dapper | Produtividade + migrations automatizadas |
| CQRS | MediatR | Implementação manual | Padronização, pipeline behaviors |
| Cache | Redis | In-Memory Cache | Compartilhado entre réplicas |
| Logs | Serilog + Seq | ELK Stack | Simplicidade para ambiente dev |
| Observabilidade | OpenTelemetry + Prometheus/Grafana | DataDog, New Relic | Open source, vendor-neutral |
| Resiliência | Polly | Custom retry logic | Padrão de mercado para .NET |
| Garantia de entrega | Transactional Outbox (EF Core + background processor) | Publish direto após SaveChanges | Atomicidade entre DB write e broker publish |
| Processamento único | Idempotency Key (índice filtrado) + Idempotent Consumer (ledger) | Nenhuma proteção | Evita duplicatas em retries e redeliveries |
| Autenticação | API Key via header `X-Api-Key` | OAuth2/JWT, Basic Auth | Viável sem IdP externo; OAuth2 planejado para produção |
| **Auditoria** | **`AuditBehavior` (MediatR pipeline) + `AuditLog` (SharedKernel)** | **Sem auditoria / log simples** | **Sistema financeiro: toda operação de escrita deve ser rastreable** |

> Documentação completa de cada decisão: [docs/adr/](docs/adr/)

---

## Roadmap e Evoluções Futuras

### ✅ Já implementado

- [x] **Transactional Outbox Pattern** — lançamento e evento persistidos atomicamente; `OutboxProcessor` publica no broker em background com retry
- [x] **Idempotency Key** — chave opcional no comando `CriarLancamento`; índice único filtrado previne duplicatas em corridas; consumidores verificam `EVENTOS_PROCESSADOS` antes de aplicar projeção
- [x] **Autenticação por API Key** — middleware `ApiKeyAuthMiddleware` em ambos os serviços; chave configurável via `appsettings` / variável de ambiente
- [x] **Security Headers** — `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection` em todas as respostas
- [x] **Auditoria persistida** — `AuditBehavior` intercepta automaticamente todos os Commands no Lançamentos; consumers do Consolidado registram audit após processar cada evento; tabela `AuditLogs` em ambos os bancos com índices em `OcorridoEm`, `Operacao` e `CorrelationId`
- [x] **Testes de carga k6 baseline** — valida RNF de 50 req/s (Consolidado) e p95 < 200ms (Lançamentos); ver `tests/k6/`
- [x] **Trade-offs documentados** — todos os 6 ADRs possuem seção `## Trade-off Decisório` explícita

### 🔜 Próximas evoluções

- [ ] **Rastreamento distribuído com Jaeger/Tempo** — integrar exportador OTLP do OpenTelemetry para traces end-to-end entre Lançamentos API, RabbitMQ e Consolidado API
- [ ] **Testes k6 stress/soak/spike** — determinar limiares de degradação, pontos de escalonamento e comportamento sob carga sustentada
- [ ] **Endpoint de consulta de auditoria** — `GET /api/auditoria` com filtros por operação, período e correlation-id
- [ ] **OAuth2/JWT com Keycloak ou Azure AD B2C** — autenticação delegada, autorização por escopos, substituindo a API Key em produção
- [ ] **Kubernetes** com HPA para escalonamento automático do serviço Consolidado
- [ ] **Event Sourcing** para histórico completo e imutável de todos os lançamentos
- [ ] **Relatórios avançados**: saldo por categoria, tendências, projeções
- [ ] **Notificações**: alertas quando saldo ficar negativo
- [ ] **Integração com ERP**: exportar consolidado para sistemas contábeis
- [ ] **Multi-tenancy**: suporte a múltiplos comerciantes na mesma instância
- [ ] **Mobile App**: React Native para registro de lançamentos offline

---

## 📄 Licença

MIT License — veja [LICENSE](LICENSE) para detalhes.

---

*Desenvolvido como desafio técnico para Arquiteto de Soluções.*
*Documentação completa disponível em [`docs/`](docs/).*
