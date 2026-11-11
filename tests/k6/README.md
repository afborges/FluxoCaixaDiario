# Testes de Carga — k6

## Visão Geral

Esta pasta contém scripts de teste de carga usando [k6](https://k6.io), uma ferramenta
open-source para testes de performance de APIs HTTP.

Os testes cobrem seis objetivos:

| Objetivo | Script | Descrição |
|----------|--------|-----------|
| **Baseline de pico** | `lancamentos-baseline.js` | Verifica se Lançamentos atende ~50 req/s por 5 minutos |
| **Leitura sustentada** | `consolidado-baseline.js` | Verifica se Consolidado atende 100 req/s com cache |
| **Estabilidade prolongada (Soak)** | `lancamentos-soak.js` | Detecta memory leaks e esgotamento de pool sob 40 VUs por 30m+ |
| **Estabilidade prolongada (Soak)** | `consolidado-soak.js` | Detecta degradação progressiva de cache Redis sob 100 req/s por 30m+ |
| **Pico súbito + recuperação (Spike)** | `lancamentos-spike.js` | Valida comportamento e recuperação após pico de 300 VUs (×6) |
| **Pico súbito + recuperação (Spike)** | `consolidado-spike.js` | Valida comportamento e recuperação após pico de 600 req/s (×6) |

---

## Pré-Requisitos

1. **k6 instalado**
   ```bash
   # Windows (via Chocolatey)
   choco install k6

   # Windows (via winget)
   winget install k6

   # Linux
   sudo apt-get install k6

   # macOS
   brew install k6
   ```

2. **Docker Compose rodando**
   ```bash
   docker compose up -d
   ```

3. **Aguardar serviços ficarem saudáveis**
   ```bash
   # Checar health dos dois serviços
   curl http://localhost:8080/health
   ```

---

## Como Executar

### Teste de Lancamentos (Baseline de pico)

```bash
# Execução padrão (porta 8080 via Nginx gateway)
k6 run tests/k6/lancamentos-baseline.js

# Apontar para outro ambiente
k6 run -e LANCAMENTOS_URL=http://localhost:5010 \
       -e API_KEY=minha-chave \
       tests/k6/lancamentos-baseline.js

# Com saída JSON para análise posterior
k6 run --out json=tests/k6/results/lancamentos-$(date +%Y%m%d-%H%M).json \
       tests/k6/lancamentos-baseline.js
```

### Teste de Consolidado (Leitura sustentada)

```bash
# Executar APÓS o teste de Lancamentos (para ter dados no banco)
k6 run tests/k6/consolidado-baseline.js

# Com saída JSON
k6 run --out json=tests/k6/results/consolidado-$(date +%Y%m%d-%H%M).json \
       tests/k6/consolidado-baseline.js
```

### Teste de Estabilidade Prolongada — Lançamentos (Soak)

```bash
# Duração padrão: 5m aquecimento + 30m soak + 5m desaquecimento
k6 run tests/k6/lancamentos-soak.js

# Recomendado para produção: 4h de soak
k6 run -e SOAK_DURATION=4h tests/k6/lancamentos-soak.js

# Com saída JSON
k6 run --out json=tests/k6/results/lancamentos-soak-$(date +%Y%m%d-%H%M).json \
       tests/k6/lancamentos-soak.js
```

### Teste de Pico + Recuperação — Lançamentos (Spike)

```bash
# Pico padrão: 300 VUs (×6 do baseline), janela de recuperação de 3 minutos
k6 run tests/k6/lancamentos-spike.js

# Para intensificar o pico
k6 run -e SPIKE_VUS=500 tests/k6/lancamentos-spike.js

# Com saída JSON
k6 run --out json=tests/k6/results/lancamentos-spike-$(date +%Y%m%d-%H%M).json \
       tests/k6/lancamentos-spike.js
```

### Teste de Estabilidade Prolongada — Consolidado (Soak)

```bash
# Executar APÓS os testes de Lançamentos (para ter dados no banco)
k6 run tests/k6/consolidado-soak.js

# Para produção (4h)
k6 run -e SOAK_DURATION=4h tests/k6/consolidado-soak.js

# Com saída JSON
k6 run --out json=tests/k6/results/consolidado-soak-$(date +%Y%m%d-%H%M).json \
       tests/k6/consolidado-soak.js
```

### Teste de Pico + Recuperação — Consolidado (Spike)

```bash
# Pico padrão: 600 req/s (×6 do baseline), janela de recuperação de 3 minutos
k6 run tests/k6/consolidado-spike.js

# Para intensificar o pico
k6 run -e SPIKE_RATE=1000 tests/k6/consolidado-spike.js

# Com saída JSON
k6 run --out json=tests/k6/results/consolidado-spike-$(date +%Y%m%d-%H%M).json \
       tests/k6/consolidado-spike.js
```

---

## Thresholds de Aceitação

### Lancamentos API
| Métrica | Threshold | RNF | Justificativa |
|---------|-----------|-----|---------------|
| `latencia_criar_lancamento` p95 | < 300ms | RNF-04 | Meta operacional; P99 é o critério formal |
| `latencia_criar_lancamento` p99 | < 500ms | **RNF-04** | Critério de aceitação formal |
| `latencia_listar_lancamentos` p95 | < 200ms | — | Leitura simples com paginação |
| `erros` | < 1% | RNF-03 | Tolerância operacional mínima |

### Consolidado API
| Métrica | Threshold | RNF | Justificativa |
|---------|-----------|-----|---------------|
| `latencia_consolidado_dia` p95 | < 100ms | RNF-05 | Cache Redis deve servir a maioria dos requests |
| `latencia_consolidado_dia` p99 | < 200ms | **RNF-05** | Critério de aceitação formal (inclui cache miss) |
| `latencia_historico` p95 | < 300ms | — | Consulta de até 30 dias sem cache dedicado |
| `erros` | < 0.5% | RNF-03 | Leitura deve ser mais estável que escrita |

### Lançamentos — Soak Test
| Métrica | Threshold | Justificativa |
|---------|-----------|---------------|
| `latencia_criar_lancamento` p99 | < 500ms | Deve manter-se estável ao longo de toda a duração |
| `latencia_listar_lancamentos` p95 | < 200ms | Degradação progressiva indica esgotamento de pool |
| `erros` | < 1% | Taxa contínua; pico pontual acima de 1% sinaliza vazamento |

### Lançamentos — Spike Test
| Métrica | Threshold | Justificativa |
|---------|-----------|---------------|
| `latencia_criar_lancamento` p99 | < 1000ms | Tolerância 2× durante o pico |
| `erros` | < 5% | Burst traffic; RNF-03 é o limite formal |
| `latencia_recuperacao` p95 | **< 300ms** | **Critério de recuperação** — igual ao baseline |
| `latencia_recuperacao` p99 | **< 500ms** | **Critério de recuperação** — igual ao RNF-04 |

### Consolidado — Soak Test
| Métrica | Threshold | Justificativa |
|---------|-----------|---------------|
| `latencia_consolidado_dia` p95 | < 100ms | Cache deve manter-se aquecido ao longo do tempo |
| `latencia_consolidado_dia` p99 | < 200ms | Degradação indica saturação de Redis ou conexões |
| `erros` | < 0.5% | Leitura deve ser mais estável que escrita |

### Consolidado — Spike Test
| Métrica | Threshold | Justificativa |
|---------|-----------|---------------|
| `latencia_consolidado_dia` p99 | < 500ms | Tolerância durante pico de leitura |
| `erros` | < 2% | Burst de dashboards simultâneos |
| `latencia_recuperacao` p95 | **< 100ms** | **Critério de recuperação** — igual ao baseline |
| `latencia_recuperacao` p99 | **< 200ms** | **Critério de recuperação** — igual ao RNF-05 |

---

## Interpretando os Resultados

### Resultado esperado (sistema saudável)
```
✓ checks.........................: 100.00%
✓ erros.........................: 0.00%
✓ latencia_criar_lancamento p95 : 150ms   ← < 500ms ✅
✓ latencia_listar_lancamentos p95: 45ms   ← < 200ms ✅
```

### Sinais de degradação que indicam necessidade de escala
| Sintoma | Causa provável | Ação de escala |
|---------|----------------|----------------|
| p95 criação > 500ms | SQL Server sobrecarregado | Escalar réplica de leitura ou shard |
| p95 consolidado > 100ms | Redis saturado ou cache miss rate alto | Aumentar memória Redis ou ajustar TTL |
| Taxa de erro > 1% | Esgotamento de conexões | Aumentar pool de conexões / escalar instâncias |
| RabbitMQ queue depth crescendo | Consumer não acompanha producer | Escalar instâncias do Consolidado |

### Verificar tamanho da fila RabbitMQ durante o teste
```bash
# Acompanhar filas em tempo real
watch -n2 'curl -s -u guest:guest http://localhost:15672/api/queues | \
  python3 -m json.tool | grep -E "(name|messages_ready)"'
```

---

## Evolução dos Testes

### Status de implementação

| Tipo | Scripts | Status |
|------|---------|--------|
| **Baseline** | `lancamentos-baseline.js`, `consolidado-baseline.js` | ✅ Implementado |
| **Soak** (estabilidade prolongada) | `lancamentos-soak.js`, `consolidado-soak.js` | ✅ Implementado |
| **Spike** (pico + recuperação) | `lancamentos-spike.js`, `consolidado-spike.js` | ✅ Implementado |
| **Stress** (ponto de ruptura) | — | 🗓 Roadmap |
| **Breakpoint + HPA** | — | 🗓 Roadmap |

### Testes ainda no roadmap

1. **Stress Test** — aumentar carga progressivamente até o sistema falhar
   ```js
   // Encontra o ponto de ruptura (breaking point)
   stages: [
     { duration: '2m', target: 100 },
     { duration: '2m', target: 200 },
     { duration: '2m', target: 400 },
     { duration: '2m', target: 800 },  // até falhar
   ]
   ```

2. **Breakpoint Test com escalamento automático** — para dimensionar HPA no Kubernetes
   - Executar stress test enquanto monitora Prometheus/Grafana
   - Identificar o threshold de CPU/memória que antecede degradação
   - Configurar HPA com target ligeiramente abaixo desse threshold

### Como usar Prometheus + Grafana durante os testes
```bash
# k6 pode enviar métricas diretamente para Prometheus remote write
k6 run --out=experimental-prometheus-rw tests/k6/lancamentos-baseline.js

# Ou gerar JSON e importar no Grafana via dashboard k6 (ID: 2587)
k6 run --out json=resultado.json tests/k6/lancamentos-baseline.js
```

---

## Estrutura de Arquivos

```
tests/k6/
├── lancamentos-baseline.js    ← Baseline: escrita ~50 req/s por 5 min
├── lancamentos-soak.js        ← Soak: 40 VUs por 30m+ (estabilidade prolongada)
├── lancamentos-spike.js       ← Spike: pico 300 VUs (×6) + validação de recuperação
├── consolidado-baseline.js    ← Baseline: leitura 100 req/s com cache por 5 min
├── consolidado-soak.js        ← Soak: 100 req/s por 30m+ (estabilidade prolongada)
├── consolidado-spike.js       ← Spike: pico 600 req/s (×6) + validação de recuperação
├── results/                   ← Pasta para JSONs de resultado (git-ignored)
└── README.md                  ← Este arquivo
```

> **Nota:** A pasta `results/` deve ser incluída no `.gitignore` para não versionar
> resultados de testes locais.
