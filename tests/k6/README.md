# Testes de Carga — k6

## Visão Geral

Esta pasta contém scripts de teste de carga usando [k6](https://k6.io), uma ferramenta
open-source para testes de performance de APIs HTTP.

Os testes cobrem dois objetivos:

| Objetivo | Script | Descrição |
|----------|--------|-----------|
| **Baseline de pico** | `lancamentos-baseline.js` | Verifica se Lancamentos atende ~50 req/s por 5 minutos |
| **Leitura sustentada** | `consolidado-baseline.js` | Verifica se Consolidado atende 100 req/s com cache |

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

---

## Thresholds de Aceitação

### Lancamentos API
| Métrica | Threshold | Justificativa |
|---------|-----------|---------------|
| `latencia_criar_lancamento` p95 | < 500ms | Criação de lançamento = escrita em banco + enfileiramento |
| `latencia_listar_lancamentos` p95 | < 200ms | Leitura simples com paginação |
| `erros` | < 1% | Tolerância operacional mínima |

### Consolidado API
| Métrica | Threshold | Justificativa |
|---------|-----------|---------------|
| `latencia_consolidado_dia` p95 | < 100ms | Cache Redis deve servir a maioria dos requests |
| `latencia_consolidado_dia` p99 | < 300ms | Inclui cache miss (SQL query) |
| `latencia_historico` p95 | < 300ms | Consulta de até 30 dias sem cache dedicado |
| `erros` | < 0.5% | Leitura deve ser mais estável que escrita |

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

## Evolução Futura dos Testes

Os testes acima são apenas o **baseline de aceitação** — verificam se o sistema
atende o mínimo esperado. Para definir **quando e como escalar**, são recomendados
testes mais intensos que ainda não foram implementados:

### Testes recomendados para roadmap

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

2. **Soak Test** — carga moderada por período longo (memory leaks, connection pool exhaustion)
   ```js
   // 80% da carga por 4 horas
   stages: [
     { duration: '10m', target: 40 },
     { duration: '4h',  target: 40 },
     { duration: '10m', target: 0  },
   ]
   ```

3. **Spike Test** — pico súbito (simulação de pagamento de salários, abertura de mercado)
   ```js
   stages: [
     { duration: '10s', target: 0   },
     { duration: '10s', target: 500 },  // spike imediato
     { duration: '3m',  target: 500 },
     { duration: '10s', target: 0   },
   ]
   ```

4. **Breakpoint Test com escalamento automático** — para dimensionar HPA no Kubernetes
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
├── lancamentos-baseline.js    ← Teste de escrita (criar/listar lançamentos)
├── consolidado-baseline.js    ← Teste de leitura (consolidado com cache)
├── results/                   ← Pasta para JSONs de resultado (git-ignored)
└── README.md                  ← Este arquivo
```

> **Nota:** A pasta `results/` deve ser incluída no `.gitignore` para não versionar
> resultados de testes locais.
