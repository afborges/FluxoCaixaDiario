# Arquitetura Enterprise — Fluxo de Caixa Diário

> **Framework:** TOGAF-Aligned (ADM Phase B — Business Architecture / Phase C — Information Systems)
> **Versão:** 1.1.0 | **Data:** Junho 2026
> **Audience:** CTO, Diretores, Enterprise Architecture Board, Gestores de TI

---

## Sumário

1. [Visão Estratégica de Negócios](#1-visão-estratégica-de-negócios)
2. [Drivers Estratégicos](#2-drivers-estratégicos)
3. [Mapa de Capacidades de Negócio](#3-mapa-de-capacidades-de-negócio)
4. [Mapa de Domínios Funcionais](#4-mapa-de-domínios-funcionais)
5. [Cadeia de Valor](#5-cadeia-de-valor)
6. [Bounded Contexts (DDD)](#6-bounded-contexts-ddd)
7. [Mapa de Aplicações](#7-mapa-de-aplicações)
8. [Arquitetura de Dados Corporativa](#8-arquitetura-de-dados-corporativa)
9. [Arquitetura de Integração Corporativa](#9-arquitetura-de-integração-corporativa)
10. [Arquitetura Tecnológica de Referência](#10-arquitetura-tecnológica-de-referência)
11. [Segurança e Conformidade](#11-segurança-e-conformidade)
12. [Modelo de Governança](#12-modelo-de-governança)
13. [Análise de Gaps (AS-IS vs TO-BE)](#13-análise-de-gaps-as-is-vs-to-be)

---

## 1. Visão Estratégica de Negócios

### 1.1 Contexto Organizacional

O comerciante opera um negócio de varejo/serviços que necessita de **controle financeiro rigoroso e em tempo real**. A ausência de uma solução digital integrada resulta em:

- **Perda de visibilidade** sobre o fluxo de caixa intradiário
- **Decisões financeiras tardias** baseadas em dados do dia anterior
- **Risco operacional** por falta de alertas de saldo negativo
- **Esforço manual** em consolidação de dados e geração de relatórios

### 1.2 Visão de Futuro (TO-BE)

> *"Um comerciante com controle financeiro digital, em tempo real, que toma decisões baseadas em dados atualizados, com total rastreabilidade e disponibilidade contínua."*

---

## 2. Drivers Estratégicos

```mermaid
mindmap
    root((Fluxo de\nCaixa Diário))
        Eficiência Operacional
            Automação de relatórios
            Eliminação de planilhas manuais
            Consolidação automática
        Disponibilidade
            Lançamentos 24/7
            Zero downtime
            Recuperação automática
        Inteligência de Negócios
            Visão em tempo real
            Histórico consolidado
            Tendências de caixa
        Conformidade e Auditoria
            Rastreabilidade total
            Log imutável de transações
            Conformidade fiscal
        Escalabilidade
            Múltiplos comerciantes
            Alto volume de lançamentos
            Picos de consultas
```

| Driver | Impacto no Negócio | Prioridade |
|--------|-------------------|-----------|
| Controle financeiro em tempo real | Redução de risco de inadimplência | 🔴 Alta |
| Disponibilidade 99.9% | Operação contínua sem interrupções | 🔴 Alta |
| Consolidação automática | Redução de 80% do trabalho manual | 🟡 Média |
| Auditabilidade | Conformidade regulatória | 🟡 Média |
| Escalabilidade multi-comerciante | Crescimento da base de clientes | 🟢 Futura |

---

## 3. Mapa de Capacidades de Negócio

> O Mapa de Capacidades de Negócio (Business Capability Map) representa **o que** a organização faz, independentemente de como é feito. É a base para o alinhamento entre Negócio e TI.

```mermaid
graph TD
    subgraph "Nível 1 — Capacidades Primárias"
        subgraph "GESTÃO FINANCEIRA"
            C1["💰 Controle de\nLançamentos\n(CORE)"]
            C2["📊 Consolidação\nDiária\n(CORE)"]
            C3["📈 Análise e\nRelatórios\n(SUPORTE)"]
            C4["🔔 Alertas e\nNotificações\n(SUPORTE)"]
        end

        subgraph "GESTÃO OPERACIONAL"
            C5["👤 Gestão de\nUsuários\n(GENÉRICO)"]
            C6["🔐 Autenticação\ne Autorização\n(GENÉRICO)"]
            C7["📋 Auditoria\ne Compliance\n(SUPORTE)"]
        end

        subgraph "INFRAESTRUTURA DIGITAL"
            C8["☁️ Hospedagem\ne Deploy\n(GENÉRICO)"]
            C9["🔍 Monitoramento\ne Observabilidade\n(GENÉRICO)"]
            C10["🔗 Integração\ncom Sistemas\n(SUPORTE)"]
        end
    end

    style C1 fill:#c62828,color:#fff
    style C2 fill:#c62828,color:#fff
    style C3 fill:#1565c0,color:#fff
    style C4 fill:#1565c0,color:#fff
    style C5 fill:#2e7d32,color:#fff
    style C6 fill:#2e7d32,color:#fff
    style C7 fill:#1565c0,color:#fff
    style C8 fill:#2e7d32,color:#fff
    style C9 fill:#2e7d32,color:#fff
    style C10 fill:#1565c0,color:#fff
```

**Legenda:**
- 🔴 **CORE** — Diferencial competitivo, construir internamente
- 🔵 **SUPORTE** — Importante mas não diferencial, construir ou comprar
- 🟢 **GENÉRICO** — Commodity, usar serviços gerenciados ou off-the-shelf

### 3.1 Decomposição das Capacidades Core

#### Capacidade C1 — Controle de Lançamentos

| Sub-capacidade | Descrição | Nível de Maturidade |
|----------------|-----------|---------------------|
| Registro de Créditos | Registrar entradas financeiras | 🟢 Implementado |
| Registro de Débitos | Registrar saídas financeiras | 🟢 Implementado |
| Cancelamento de Lançamentos | Anular lançamentos registrados | 🟢 Implementado |
| Categorização | Classificar por tipo/categoria | 🔴 Planejado (v2) |
| Lançamentos Recorrentes | Lançamentos automáticos periódicos | 🔴 Planejado (v3) |

#### Capacidade C2 — Consolidação Diária

| Sub-capacidade | Descrição | Nível de Maturidade |
|----------------|-----------|---------------------|
| Agregação por Data | Somar créditos e débitos do dia | 🟢 Implementado |
| Cálculo de Saldo | Saldo líquido diário | 🟢 Implementado |
| Histórico Consolidado | Consulta por período | 🟢 Implementado |
| Consolidação em Tempo Real | Atualização imediata após lançamento | 🟡 Eventual (< 5s) |
| Fechamento Contábil | Bloquear edição de dias fechados | 🔴 Planejado (v2) |

---

## 4. Mapa de Domínios Funcionais

```mermaid
graph LR
    subgraph "Domínio de Lançamentos (Core Domain)"
        D1_E1["Lancamento\n(Aggregate Root)"]
        D1_E2["TipoLancamento\n(Enum: Credito/Debito)"]
        D1_E3["StatusLancamento\n(Enum: Confirmado/Cancelado)"]
        D1_E4["Valor\n(Value Object)"]
        D1_E5["LancamentoCriadoEvent\n(Domain Event)"]
        D1_E6["LancamentoCanceladoEvent\n(Domain Event)"]
    end

    subgraph "Domínio de Consolidado (Supporting Domain)"
        D2_E1["ConsolidadoDiario\n(Aggregate Root)"]
        D2_E2["PeriodoConsolidacao\n(Value Object)"]
        D2_E3["SaldoDiario\n(Value Object)"]
    end

    subgraph "Domínio de Identidade (Generic Domain)"
        D3_E1["Usuario"]
        D3_E2["Role / Permissão"]
        D3_E3["Token JWT"]
    end

    subgraph "Domínio de Auditoria (Generic Domain)"
        D4_E1["AuditLog"]
        D4_E2["AuditEvent"]
    end

    D1_E1 -->|"gera"| D1_E5
    D1_E1 -->|"gera"| D1_E6
    D1_E5 -->|"atualiza"| D2_E1
    D1_E6 -->|"atualiza"| D2_E1
    D3_E1 -->|"cria"| D1_E1
    D1_E1 -->|"registra"| D4_E1
```

### 4.1 Classificação dos Domínios (DDD Strategic Design)

| Domínio | Tipo DDD | Complexidade | Justificativa |
|---------|----------|-------------|---------------|
| **Lançamentos** | Core Domain | Alta | Diferencial de negócio, regras específicas |
| **Consolidado Diário** | Supporting Domain | Média | Suporta o Core, modelo mais simples |
| **Identidade/Auth** | Generic Domain | Baixa | Commodity (usar Keycloak/Azure AD) |
| **Auditoria** | Generic Domain | Baixa | Cross-cutting concern |

---

## 5. Cadeia de Valor

```mermaid
graph LR
    subgraph "Atividades Primárias"
        A1["📥 Captura do\nLançamento\n\nEntrada de dados\nvalidação básica"]
        A2["✅ Processamento\ndo Lançamento\n\nValidação de negócio\nPersistência"]
        A3["📡 Publicação\nde Evento\n\nNotificação assíncrona\nDesacoplamento"]
        A4["🔄 Consolidação\nDiária\n\nAgregação automática\nAtualização de saldo"]
        A5["📊 Relatório\nConsolidado\n\nConsulta com cache\nExibição ao usuário"]
    end

    subgraph "Atividades de Suporte"
        S1["🔐 Segurança\n& Identidade"]
        S2["📋 Auditoria\n& Compliance"]
        S3["🔍 Monitoramento\n& Observabilidade"]
        S4["⚙️ Infraestrutura\n& DevOps"]
    end

    A1 --> A2 --> A3 --> A4 --> A5

    S1 -.->|"suporta"| A1
    S1 -.->|"suporta"| A2
    S2 -.->|"suporta"| A2
    S2 -.->|"suporta"| A4
    S3 -.->|"suporta"| A2
    S3 -.->|"suporta"| A4
    S4 -.->|"suporta"| A1
    S4 -.->|"suporta"| A5
```

**Entrega de Valor ao Comerciante:**

```
Registro Rápido    →    Processamento Confiável    →    Relatório Preciso
(< 500ms)               (durabilidade garantida)         (< 200ms)
```

---

## 6. Bounded Contexts (DDD)

```mermaid
graph TB
    subgraph "BC: Lançamentos"
        direction TB
        BC1_A["API Gateway\n(Anti-Corruption Layer)"]
        BC1_B["Lancamento Aggregate"]
        BC1_C["LancamentoRepository"]
        BC1_D["LancamentosDB"]
    end

    subgraph "BC: Consolidado Diário"
        direction TB
        BC2_A["Consumer\n(ACL - traduz evento)"]
        BC2_B["ConsolidadoDiario Aggregate"]
        BC2_C["ConsolidadoRepository"]
        BC2_D["ConsolidadoDB + Redis"]
    end

    subgraph "BC: Identidade"
        BC3["Keycloak / Azure AD\n(External System)"]
    end

    subgraph "Shared Kernel"
        SK["Integration Events\n(contratos compartilhados)\n\nLancamentoCriadoIntegrationEvent\nLancamentoCanceladoIntegrationEvent"]
    end

    BC1_B -->|"publica"| SK
    SK -->|"consome"| BC2_A
    BC3 -->|"JWT Token"| BC1_A
    BC3 -->|"JWT Token"| BC2_A

    style SK fill:#ffa000,color:#fff
    style BC1_B fill:#c62828,color:#fff
    style BC2_B fill:#c62828,color:#fff
    style BC3 fill:#2e7d32,color:#fff
```

### 6.1 Mapa de Contextos (Context Map)

| Contexto | Tipo de Relacionamento | Com | Mecanismo |
|----------|----------------------|-----|-----------|
| Lançamentos → Consolidado | **Customer/Supplier** (Upstream/Downstream) | Consolidado consome do Lançamentos | Integration Events via RabbitMQ |
| Lançamentos → Identidade | **Conformist** | Lançamentos respeita contratos do IdP | JWT Token |
| Consolidado → Identidade | **Conformist** | Consolidado respeita contratos do IdP | JWT Token |
| Lançamentos/Consolidado → **Shared Kernel** | **Shared Kernel** | Contratos de eventos compartilhados | NuGet Package |

---

## 7. Mapa de Aplicações

```mermaid
graph TB
    subgraph "Portfolio de Aplicações — Fluxo de Caixa Diário"

        subgraph "Canal de Acesso"
            APP1["🌐 Web App\n(futuro: React/Angular)"]
            APP2["📱 Mobile App\n(futuro: React Native)"]
            APP3["🖥️ CLI Tool\n(futuro: integrações batch)"]
        end

        subgraph "API Layer"
            APP4["🔀 API Gateway\nnginx/Ocelot"]
        end

        subgraph "Microsserviços (Core)"
            APP5["⚡ Lancamentos API\nASP.NET Core 10"]
            APP6["📊 Consolidado API\nASP.NET Core 10"]
        end

        subgraph "Middleware"
            APP7["🐰 RabbitMQ\nMessage Broker"]
            APP8["⚡ Redis\nCache"]
        end

        subgraph "Dados"
            APP9["🗄️ Lancamentos DB\nSQL Server"]
            APP10["🗄️ Consolidado DB\nSQL Server"]
        end

        subgraph "Serviços Externos (futuro)"
            APP11["🔐 Identity Provider\nKeycloak / Azure AD"]
            APP12["📧 Notificações\nSendGrid / Twilio"]
            APP13["📊 BI Platform\nPower BI / Tableau"]
        end

        subgraph "Observabilidade"
            APP14["📋 Seq\nLogs"]
            APP15["📈 Prometheus/Grafana\nMétricas"]
            APP16["🔍 Jaeger\nTracing"]
        end
    end

    APP1 --> APP4
    APP2 --> APP4
    APP3 --> APP4
    APP4 --> APP5
    APP4 --> APP6
    APP5 --> APP9
    APP5 --> APP7
    APP7 --> APP6
    APP6 --> APP10
    APP6 --> APP8
    APP11 --> APP4
    APP5 --> APP14
    APP6 --> APP14
    APP5 --> APP15
    APP6 --> APP15
    APP5 --> APP16
    APP6 --> APP16
    APP6 --> APP13
    APP5 --> APP12
```

### 7.1 Matriz de Aplicações por Capacidade

| Capacidade | Aplicação Suporte | Ciclo de Vida | Criticidade |
|------------|------------------|---------------|-------------|
| Registro de Lançamentos | Lancamentos API | Ativo | 🔴 Crítico |
| Consulta de Saldo | Consolidado API | Ativo | 🔴 Crítico |
| Autenticação | IdP (futuro) | Planejado | 🔴 Crítico |
| Relatórios Avançados | BI Platform (futuro) | Planejado | 🟡 Importante |
| Notificações | Notificações (futuro) | Planejado | 🟡 Importante |
| Monitoramento | Prometheus/Grafana | Ativo | 🟡 Importante |
| Auditoria de Logs | Seq | Ativo | 🟡 Importante |

---

## 8. Arquitetura de Dados Corporativa

### 8.1 Domínios de Dados

```mermaid
graph LR
    subgraph "Data Domain: Operacional"
        DD1["Lancamentos\n(Source of Truth)\n\nTabela: Lancamentos\n- dados transacionais\n- histórico completo\n- auditoria integrada"]
    end

    subgraph "Data Domain: Analítico"
        DD2["Consolidado Diário\n(Projeção/Read Model)\n\nTabela: ConsolidadoDiario\n- dados agregados\n- otimizado para leitura\n- eventual consistency"]
    end

    subgraph "Cache Layer"
        DD3["Redis\n(Cache de Consulta)\n\nChave: consolidado:{data}\nTTL: 5 minutos\nFormato: JSON"]
    end

    DD1 -->|"Integration Event\n(eventual)"| DD2
    DD2 -->|"Cache-Aside"| DD3
```

### 8.2 Políticas de Retenção de Dados

| Dado | Retenção | Justificativa | Storage |
|------|----------|--------------|---------|
| Lançamentos | 10 anos | Conformidade fiscal/contábil | SQL Server (particionamento por ano) |
| Consolidado Diário | 10 anos | Histórico de negócios | SQL Server |
| Audit Logs | 5 anos | Rastreabilidade regulatória | SQL Server / Cold Storage |
| Logs de Aplicação | 90 dias | Diagnóstico operacional | Seq / Azure Monitor |
| Cache Redis | 5 minutos | Dados recentes e frequentes | Redis (TTL) |

### 8.3 Qualidade e Integridade dos Dados

| Regra | Implementação | Camada |
|-------|--------------|--------|
| Valor deve ser positivo | ValueObject `Valor` | Domínio |
| Data não pode ser futura | FluentValidation | Aplicação |
| Lançamento cancelado não pode ser re-ativado | State machine no Aggregate | Domínio |
| Consolidado é idempotente por lançamento | Chave de idempotência | Infraestrutura |
| Campos de auditoria obrigatórios | Interceptor EF Core | Infraestrutura |

---

## 9. Arquitetura de Integração Corporativa

### 9.1 Topologia de Integração

```mermaid
graph TB
    subgraph "Integrações Internas (Event-Driven)"
        LA["Lancamentos API\n(Publisher)"]
        RMQ["RabbitMQ\nExchange: fluxo-caixa.lancamentos\nExchange type: Topic"]
        CA["Consolidado API\n(Consumer)"]
        LA -->|"lancamento.criado\nlancamento.cancelado"| RMQ
        RMQ -->|"#.lancamento.*"| CA
    end

    subgraph "Integrações Externas (REST — Futuro)"
        GW["API Gateway\n(OAuth2 enforced)"]
        ERP["ERP Legado\n(webhook receiver)"]
        BI["BI Platform\n(data consumer)"]
        GW -->|"REST + JWT"| LA
        GW -->|"REST + JWT"| CA
        LA -->|"webhook\nevento"| ERP
        CA -->|"dados consolidados\nperiodicamente"| BI
    end
```

### 9.2 Padrões de Integração Adotados

| Padrão (EIP) | Descrição | Uso no Sistema |
|-------------|-----------|----------------|
| **Message Channel** | Canal de comunicação entre produtores/consumidores | Exchange RabbitMQ |
| **Message Router** | Roteia mensagens por tipo | Topic Exchange + routing keys |
| **Dead Letter Channel** | Canal para mensagens com falha | DLQ RabbitMQ |
| **Idempotent Receiver** | Garante processamento único | LancamentoId como chave de idempotência |
| **Competing Consumers** | Múltiplas réplicas consomem a mesma fila | MassTransit + RabbitMQ |
| **Event-Driven Consumer** | Consome em resposta a evento | MassTransit IConsumer<T> |

---

## 10. Arquitetura Tecnológica de Referência

### 10.1 Stack Tecnológico

```mermaid
graph TB
    subgraph "Frontend (Futuro)"
        T1["React / Angular\nTypeScript"]
    end

    subgraph "API & Runtime"
        T2[".NET 10\nASP.NET Core\nC# 14"]
        T3["MediatR 12\n(CQRS)"]
        T4["MassTransit 8\n(Messaging)"]
        T5["FluentValidation 11\n(Validation)"]
        T6["Polly 8\n(Resilience)"]
    end

    subgraph "Persistência"
        T7["Entity Framework Core 10\n(ORM)"]
        T8["SQL Server 2022\n(Relational DB)"]
        T9["Redis 7\n(Cache)"]
    end

    subgraph "Mensageria"
        T10["RabbitMQ 3.12\n(Message Broker)"]
    end

    subgraph "Observabilidade"
        T11["Serilog\n(Structured Logging)"]
        T12["OpenTelemetry\n(Traces + Metrics)"]
        T13["Prometheus\n(Metrics Store)"]
        T14["Grafana\n(Dashboards)"]
        T15["Seq\n(Log Explorer)"]
    end

    subgraph "Infraestrutura"
        T16["Docker\n(Containers)"]
        T17["Kubernetes\n(Orquestração — futuro)"]
        T18["Nginx\n(API Gateway local)"]
    end

    T2 --- T3
    T2 --- T4
    T2 --- T5
    T2 --- T6
    T2 --- T7
    T7 --- T8
    T4 --- T10
    T2 --- T9
    T11 --- T15
    T12 --- T13
    T13 --- T14
    T2 --- T16
    T16 --- T17
```

### 10.2 Justificativa das Escolhas Tecnológicas

| Tecnologia | Justificativa | Alternativa Considerada |
|------------|--------------|------------------------|
| **.NET 10** | LTS, melhor performance do mercado (.NET > Node em I/O-bound), ecossistema maduro, suporte até nov/2028 | Java Spring Boot, Go |
| **SQL Server** | ACID completo, suporte EF Core nativo, familiar à maioria dos times .NET | PostgreSQL (igualmente válido) |
| **RabbitMQ** | Ampla adoção, fácil setup local, MassTransit nativo, durabilidade de mensagens | Apache Kafka (overhead maior), Azure Service Bus (cloud-vendor lock) |
| **Redis** | 100k+ ops/s, suporte TTL, JSON nativo, padrão de mercado para cache | Memcached (menos features), IMemoryCache (não compartilhado entre réplicas) |
| **MediatR** | Implementação CQRS idiomática em .NET, pipeline behaviors elegantes | Implementação manual (mais código, menos padronizado) |
| **MassTransit** | Abstração sobre RabbitMQ/SB/SQS, retry/error handling built-in, saga support | RabbitMQ direto (mais acoplado ao broker) |
| **Serilog** | Logging estruturado (JSON), sinks configuráveis, integração OpenTelemetry | Microsoft.Extensions.Logging puro (menos features) |
| **OpenTelemetry** | Vendor-neutral, CNCF project, suporte nativo .NET 10 | DataDog/New Relic (custo, vendor lock-in) |

---

## 11. Segurança e Conformidade

### 11.1 Modelo de Ameaças (STRIDE)

| Ameaça | Categoria STRIDE | Controle |
|--------|----------------|---------|
| Acesso não autorizado à API | **Spoofing** | **API Key via `X-Api-Key` (MVP)** + log de tentativas inválidas; OAuth2/JWT na Fase 2 |
| Manipulação de dados de lançamento | **Tampering** | TLS 1.3, validação de entrada, imutabilidade do aggregate |
| Negação de lançamentos existentes | **Repudiation** | Audit log imutável com timestamp e usuário |
| Exposição de dados financeiros | **Information Disclosure** | TDE no banco, HTTPS obrigatório, security headers, mascaramento em logs |
| Sobrecarga do serviço de consolidado | **Denial of Service** | Rate limiting, circuit breaker, cache |
| Elevação de privilégios | **Elevation of Privilege** | **Sem roles no MVP** (API Key é binário: autorizado ou não); RBAC por scope na Fase 2 |

### 11.2 Controles de Segurança por Camada

| Camada | Controles |
|--------|----------|
| **Rede** | TLS 1.3, HTTPS Redirection habilitada, firewall, VPN para acesso admin |
| **API Gateway** | Rate limiting, WAF, DDoS protection |
| **Aplicação** | **`ApiKeyAuthMiddleware`** (header `X-Api-Key`), security headers (`X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`), FluentValidation, sanitização de input |
| **Dados** | TDE (Transparent Data Encryption), colunas sensíveis criptografadas, Row-Level Security |
| **Mensageria** | TLS no AMQP, autenticação por VHOST, ACLs por fila |
| **Infraestrutura** | Principle of Least Privilege, chave via env var / secret manager (Azure Key Vault / Vault) |

> ⚠️ **Nota de evolução:** a camada de Aplicação migrará de API Key para **JWT Bearer Token (OAuth2/OIDC)** com RBAC por scope na Fase 2 de produção. Ver [ADR-006](adr/ADR-006-autenticacao-api-key.md) para o caminho de migração.

### 11.3 Conformidade Regulatória

| Regulação | Relevância | Status |
|-----------|-----------|--------|
| **LGPD** | Dados de usuários comerciantes | Parcial — requer mapeamento de dados pessoais |
| **SOC 2 Type II** | Se SaaS multi-tenant | Planejado para v3 |
| **PCI DSS** | Se houver processamento de pagamentos (não no escopo atual) | Fora do escopo MVP |

---

## 12. Modelo de Governança

### 12.1 Responsabilidades Arquiteturais

| Papel | Responsabilidade | Decisões |
|-------|----------------|---------|
| **Enterprise Architect** | Alinhamento estratégico, capacidades de negócio, portfólio | Plataforma tecnológica, domínios |
| **Solution Architect** | Arquitetura da solução, integrações, NFRs | Padrões, protocolos, ADRs |
| **Software Architect** | Design de software, padrões de código, revisão técnica | Padrões de código, frameworks internos |
| **Tech Lead** | Implementação, qualidade, testes, entrega | Bibliotecas, estrutura de código |
| **DevOps/SRE** | Infraestrutura, CI/CD, SLOs, monitoramento | Ferramentas de infra, pipelines |

### 12.2 Architecture Decision Records (ADRs)

Todo desvio arquitetural significativo deve ser registrado como ADR:
- Formato: data de registro, contexto, opções consideradas, decisão, consequências
- Localização: `/docs/adr/`
- Review: obrigatório por Solution Architect e Tech Lead
- Status: Proposto → Em revisão → Aprovado / Rejeitado / Obsoleto

---

## 13. Análise de Gaps (AS-IS vs TO-BE)

| Capacidade | AS-IS (Estado Atual) | TO-BE (Estado Alvo — MVP) | Gap | Ação |
|-----------|----------------------|--------------------------|-----|------|
| Registro de lançamentos | Manual (planilha) | API REST automatizada | 🔴 Alto | Implementado no MVP |
| Consolidação diária | Manual, fim do dia | Automática, tempo real | 🔴 Alto | Implementado no MVP |
| Disponibilidade | Baixa (arquivo local) | 99.9% (SLA) | 🔴 Alto | Implementado no MVP |
| Auditoria | Nenhuma | Log imutável completo | 🟡 Médio | Implementado no MVP |
| Segurança básica | Nenhuma | **API Key Auth + Security Headers + HTTPS** | 🟡 Médio | **Implementado no MVP** |
| Segurança avançada | Nenhuma | OAuth2/JWT + RBAC (Keycloak / Azure AD B2C) | 🟡 Médio | Planejado Fase 2 |
| Multi-comerciante | N/A | Multi-tenancy | 🟢 Baixo | Planejado Fase 3 |
| Relatórios avançados | N/A | BI integration | 🟢 Baixo | Planejado Fase 3 |
| Mobile | N/A | App nativo | 🟢 Baixo | Planejado Fase 4 |

---

*Documento alinhado ao TOGAF ADM — Architecture Development Method*
*Próxima revisão: após entrega do MVP (Fase 1)*