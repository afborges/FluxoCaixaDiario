# ADR-008 — Escalonamento Horizontal com Kubernetes e HPA

| Campo | Valor |
|-------|-------|
| **ID** | ADR-008 |
| **Título** | Escalonamento Horizontal Automático com Kubernetes HPA |
| **Status** | ✅ Aprovado |
| **Data** | Junho 2026 |
| **Autores** | Arquiteto de Soluções |
| **Revisores** | Tech Lead, SRE |

---

## Contexto

Os requisitos não funcionais exigem que o sistema suporte carga significativa sem degradação:

- **RNF-02**: O serviço de Consolidado deve suportar **≥ 100 req/s** sem degradação de performance.
- **RNF-03**: Taxa de erros < 0,5% sob carga sustentada.
- **RNF-04**: P99 de latência em Lançamentos < 500 ms.
- **RNF-05**: P99 de latência em Consolidado < 200 ms.

O ambiente Docker Compose atende ao desenvolvimento local, mas não oferece escalonamento automático. Com o crescimento da carga, réplicas adicionais são necessárias para manter os SLOs acima sem intervenção manual.

---

## Decisão

Adotar **Kubernetes** como plataforma de orquestração de contêineres com **Horizontal Pod Autoscaler (HPA)** para ambos os serviços, priorizando o `consolidado-api` por ser o componente de maior carga leitura.

### Configuração do HPA — `consolidado-api` (prioritário)

| Parâmetro | Valor | Justificativa |
|-----------|-------|---------------|
| `minReplicas` | 2 | HA garantida; elimina SPOF; distribui tráfego de entrada |
| `maxReplicas` | 5 | Capacidade de pico: 5 × ~25 req/s = 125 req/s com folga para RNF-02 |
| CPU target | 60% | Conservador; deixa headroom para picos sem atingir throttle |
| Memória target | 75% | Protege contra vazamentos acumulativos antes do OOM |
| `scaleUp.stabilizationWindow` | 30 s | Reação rápida a picos de tráfego |
| `scaleDown.stabilizationWindow` | 300 s | Evita flapping ao término de rajadas |
| `scaleUp.policy` | +2 pods / 30 s | Escalonamento agressivo para absorver picos |

### Configuração do HPA — `lancamentos-api`

| Parâmetro | Valor | Justificativa |
|-----------|-------|---------------|
| `minReplicas` | 1 | Write-path; carga menor; sem HA estrita por ser stateless |
| `maxReplicas` | 3 | Burst de gravações; OutboxProcessor é single-active por design |
| CPU target | 70% | Menos conservador que o Consolidado; workload write-bound |

### Recursos por contêiner

| Serviço | CPU request | CPU limit | Mem request | Mem limit |
|---------|-------------|-----------|-------------|-----------|
| `lancamentos-api` | 100 m | 500 m | 256 Mi | 512 Mi |
| `consolidado-api` | 200 m | 1000 m | 512 Mi | 1 Gi |

> `consolidado-api` recebe recursos maiores por ser read-heavy e manter estado de cache Redis em memória.

### Anti-afinidade de Pods

O `consolidado-api` configura `podAntiAffinity` preferencial por hostname, garantindo que réplicas sejam distribuídas em nós físicos distintos sempre que possível — eliminando ponto único de falha a nível de nó.

---

## Alternativas Consideradas

### A. Manter apenas Docker Compose com `--scale`

**Rejeitada.** `docker compose up --scale consolidado-api=3` não oferece escalonamento automático baseado em métricas, sem health-driven scheduling, sem reinicialização automática em falhas de nó.

### B. Docker Swarm

**Rejeitada.** Ecosystem menor, sem suporte a HPA baseado em métricas customizadas, roadmap de evolução limitado comparado ao Kubernetes.

### C. KEDA (Kubernetes Event-Driven Autoscaling)

**Considerada para evolução futura.** Permite escalonamento baseado em tamanho de fila RabbitMQ (métrica de negócio), mais preciso que CPU para workloads event-driven. Não implementado nesta iteração por exigir o KEDA operator instalado no cluster.

---

## Consequências

### Positivas
- Escalonamento automático reativo em ~30 s para picos de carga.
- SLOs RNF-02 a RNF-05 sustentáveis sem intervenção manual.
- Rolling updates com `maxUnavailable=0` garantem zero-downtime deployments.
- Manifests versionados em `infra/k8s/` e gerenciados via Kustomize.
- Liveness/Readiness/Startup probes em `/health` evitam roteamento para pods não prontos.
- Annotations `prometheus.io/scrape` integram ao Prometheus sem ServiceMonitor adicional.

### Negativas / Trade-offs
- Kubernetes adiciona complexidade operacional vs. Docker Compose.
- Métricas server-side (CPU/memória) exigem o **metrics-server** instalado no cluster.
- Para ambientes de desenvolvimento local, Docker Compose continua sendo o caminho recomendado.
- O `OutboxProcessor` (BackgroundService em `lancamentos-api`) não escala horizontalmente por design — múltiplas réplicas processam a mesma fila; leader election ou particionamento seria necessário para escalar writes do Outbox.

---

## Trade-off Decisório

> **Complexidade operacional do Kubernetes** vs. **cumprimento dos RNFs de performance e disponibilidade**.
>
> O HPA com target de CPU 60% para o `consolidado-api` garante que o sistema reaja a picos antes de degradar latência (P99 < 200 ms). O custo é a necessidade de um cluster K8s com metrics-server — justificado para ambientes de produção/staging, mantendo Docker Compose para desenvolvimento local.

---

## Referências

- [Kubernetes HPA — autoscaling/v2](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)
- [infra/k8s/consolidado-api/hpa.yaml](../../infra/k8s/consolidado-api/hpa.yaml)
- [infra/k8s/lancamentos-api/hpa.yaml](../../infra/k8s/lancamentos-api/hpa.yaml)
- [tests/k6/](../../tests/k6/) — testes de carga que validam RNF-02 a RNF-05
- ADR-001 — Arquitetura de Microsserviços
