/**
 * k6 Spike Test — Serviço de Consolidado Diário
 *
 * Objetivo: verificar comportamento do serviço de leitura frente a picos SÚBITOS
 * (ex: múltiplos dashboards abertos simultaneamente, batch de relatórios) e
 * validar RECUPERAÇÃO — cache Redis e latência retornam ao nível do baseline.
 *
 * Fases:
 *   1. aquecimento  (0s)       : 30s → 100 req/s  (aquece o cache)
 *   2. spike_subida (30s)      : 10s → 600 req/s  (pico súbito ×6)
 *   3. spike_pico   (40s)      : 1m  → 600 req/s  (sustentação do pico)
 *   4. spike_descida (1m40s)   : 10s → 100 req/s  (queda brusca)
 *   5. recuperacao  (1m50s)    : 3m  → 100 req/s  (janela de validação de recuperação)
 *
 * A fase "recuperacao" registra a métrica `latencia_recuperacao` com threshold
 * equivalente ao baseline para garantir retorno ao nível normal.
 *
 * Pré-requisitos:
 *   - Docker Compose UP: docker compose up -d
 *   - k6 instalado: https://k6.io/docs/get-started/installation/
 *   - Ao menos 30 dias de dados (execute lancamentos-baseline.js antes)
 *
 * Execução:
 *   k6 run tests/k6/consolidado-spike.js
 *
 * Para aumentar o pico:
 *   k6 run -e SPIKE_RATE=1000 tests/k6/consolidado-spike.js
 *
 * Para gerar relatório JSON:
 *   k6 run --out json=tests/k6/results/consolidado-spike.json tests/k6/consolidado-spike.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// ─────────────────────────────────────────────────────────────────────────────
// Métricas customizadas
// ─────────────────────────────────────────────────────────────────────────────
const errorRate           = new Rate('erros');
const latenciaConsolidado = new Trend('latencia_consolidado_dia');
const latenciaHistorico   = new Trend('latencia_historico');
const latenciaCacheHit    = new Trend('latencia_cache_hit');
const latenciaRecuperacao = new Trend('latencia_recuperacao'); // fase pós-pico

// ─────────────────────────────────────────────────────────────────────────────
// Configuração do cenário
// ─────────────────────────────────────────────────────────────────────────────
const BASE_RATE  = parseInt(__ENV.BASE_RATE  || '100'); // req/s normal
const SPIKE_RATE = parseInt(__ENV.SPIKE_RATE || '600'); // req/s no pico (×6)

export const options = {
  scenarios: {
    // Fases 1–4: aquecimento → pico → queda (ramping-arrival-rate)
    spike: {
      executor: 'ramping-arrival-rate',
      startRate: BASE_RATE,
      timeUnit: '1s',
      preAllocatedVUs: 100,
      maxVUs: 600,
      stages: [
        { duration: '30s', target: BASE_RATE  }, // 1. aquecimento (aquece cache)
        { duration: '10s', target: SPIKE_RATE }, // 2. pico súbito
        { duration: '1m',  target: SPIKE_RATE }, // 3. sustentação
        { duration: '10s', target: BASE_RATE  }, // 4. queda brusca
      ],
      exec: 'cenarioPadrao',
    },

    // Fase 5: janela de recuperação com métrica separada
    // startTime = 30s + 10s + 60s + 10s = 110s após o início
    recuperacao: {
      executor: 'constant-arrival-rate',
      rate: BASE_RATE,
      timeUnit: '1s',
      duration: '3m',
      preAllocatedVUs: 50,
      maxVUs: 200,
      startTime: '1m50s',
      exec: 'cenarioRecuperacao',
    },
  },

  // ── Thresholds de aceitação ──────────────────────────────────────────────
  thresholds: {
    // Durante o pico: tolerância expandida (cache pode ser contornado sob pressão extrema)
    latencia_consolidado_dia: ['p(99)<500'],
    latencia_historico:        ['p(95)<600'],
    // RNF-03: até 2% de erros durante o pico (burst de leitura)
    erros:           ['rate<0.02'],
    http_req_failed: ['rate<0.02'],
    // Validação de RECUPERAÇÃO: após o pico, latência deve retornar ao baseline
    // p95 < 100ms e p99 < 200ms (mesmos critérios do RNF-05)
    latencia_recuperacao: ['p(95)<100', 'p(99)<200'],
  },
};

// ─────────────────────────────────────────────────────────────────────────────
// Configuração — ajuste conforme ambiente
// ─────────────────────────────────────────────────────────────────────────────
const BASE_URL = __ENV.CONSOLIDADO_URL || 'http://localhost:8080';
const API_KEY  = __ENV.API_KEY         || 'TROQUE-ESTA-CHAVE-EM-PRODUCAO';

const HEADERS = { 'X-Api-Key': API_KEY };

// Datas de referência para simular consultas variadas
const DATAS_RECENTES = Array.from({ length: 30 }, (_, i) => {
  const d = new Date();
  d.setDate(d.getDate() - i);
  return d.toISOString().split('T')[0];
});

// ─────────────────────────────────────────────────────────────────────────────
// Cenário padrão (fases 1–4: aquecimento + pico + queda)
// ─────────────────────────────────────────────────────────────────────────────
export function cenarioPadrao() {
  const usarHoje = Math.random() < 0.7;
  const data = usarHoje
    ? DATAS_RECENTES[0]
    : DATAS_RECENTES[Math.floor(Math.random() * DATAS_RECENTES.length)];

  const t0 = Date.now();
  const resConsolidado = http.get(
    `${BASE_URL}/api/consolidado/${data}`,
    { headers: HEADERS }
  );
  const duracaoConsolidado = Date.now() - t0;

  latenciaConsolidado.add(resConsolidado.timings.duration);

  const consolidadoOk = check(resConsolidado, {
    'GET /consolidado/{data} status 200 ou 404': (r) => r.status === 200 || r.status === 404,
    'GET /consolidado/{data} resposta em < 1000ms': (r) => r.timings.duration < 1000,
  });

  if (resConsolidado.status === 200 && duracaoConsolidado < 20) {
    latenciaCacheHit.add(duracaoConsolidado);
  }

  errorRate.add(!consolidadoOk);

  if (Math.random() < 0.3) {
    const dataInicio = DATAS_RECENTES[29];
    const dataFim    = DATAS_RECENTES[0];

    const resHistorico = http.get(
      `${BASE_URL}/api/consolidado/historico?dataInicio=${dataInicio}&dataFim=${dataFim}`,
      { headers: HEADERS }
    );

    latenciaHistorico.add(resHistorico.timings.duration);

    const historicoOk = check(resHistorico, {
      'GET /consolidado/historico status 200': (r) => r.status === 200,
    });

    errorRate.add(!historicoOk);
  }

  sleep(0.01);
}

// ─────────────────────────────────────────────────────────────────────────────
// Cenário de recuperação (fase 5: valida retorno ao baseline pós-pico)
// Registra latência em métrica separada `latencia_recuperacao`.
// ─────────────────────────────────────────────────────────────────────────────
export function cenarioRecuperacao() {
  const usarHoje = Math.random() < 0.7;
  const data = usarHoje
    ? DATAS_RECENTES[0]
    : DATAS_RECENTES[Math.floor(Math.random() * DATAS_RECENTES.length)];

  const resConsolidado = http.get(
    `${BASE_URL}/api/consolidado/${data}`,
    { headers: HEADERS }
  );

  // Registrar tanto na métrica geral quanto na métrica de recuperação
  latenciaConsolidado.add(resConsolidado.timings.duration);
  latenciaRecuperacao.add(resConsolidado.timings.duration);

  const consolidadoOk = check(resConsolidado, {
    '[recuperacao] GET /consolidado/{data} status 200 ou 404': (r) => r.status === 200 || r.status === 404,
    '[recuperacao] latencia < 200ms': (r) => r.timings.duration < 200,
  });

  if (resConsolidado.status === 200 && resConsolidado.timings.duration < 20) {
    latenciaCacheHit.add(resConsolidado.timings.duration);
  }

  errorRate.add(!consolidadoOk);

  sleep(0.01);
}

// ─────────────────────────────────────────────────────────────────────────────
// Resumo final customizado
// ─────────────────────────────────────────────────────────────────────────────
export function handleSummary(data) {
  const p95       = data.metrics['latencia_consolidado_dia']?.values['p(95)'] ?? 0;
  const p99       = data.metrics['latencia_consolidado_dia']?.values['p(99)'] ?? 0;
  const p95Recup  = data.metrics['latencia_recuperacao']?.values['p(95)']     ?? 0;
  const p99Recup  = data.metrics['latencia_recuperacao']?.values['p(99)']     ?? 0;
  const cacheHits = data.metrics['latencia_cache_hit']?.values.count          ?? 0;
  const totalCons = data.metrics['latencia_consolidado_dia']?.values.count    ?? 1;
  const taxaErro  = (data.metrics['erros']?.values.rate ?? 0) * 100;
  const totalReqs = data.metrics['http_reqs']?.values.count                   ?? 0;

  const recuperouP95 = p95Recup > 0 && p95Recup < 100;
  const recuperouP99 = p99Recup > 0 && p99Recup < 200;

  console.log('\n╔══════════════════════════════════════════════════╗');
  console.log('║   RESULTADO — Consolidado Spike Test             ║');
  console.log('╠══════════════════════════════════════════════════╣');
  console.log(`║  Total de requisições    : ${String(totalReqs).padStart(10)}              ║`);
  console.log(`║  p95 consolidado (geral) : ${p95.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p99 consolidado (geral) : ${p99.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p95 recuperação pós-pico: ${p95Recup.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p99 recuperação pós-pico: ${p99Recup.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  Estimativa cache hits   : ${String(cacheHits).padStart(10)}              ║`);
  console.log(`║  Taxa de cache hit       : ${((cacheHits / totalCons) * 100).toFixed(1).padStart(7)}%             ║`);
  console.log(`║  Taxa de erro (geral)    : ${taxaErro.toFixed(2).padStart(7)}%             ║`);
  console.log(`║  Recuperou p95 < 100ms   : ${recuperouP95 ? '✅ OK   ' : '❌ FALHA'} (${p95Recup.toFixed(1)}ms)     ║`);
  console.log(`║  Recuperou p99 < 200ms   : ${recuperouP99 ? '✅ OK   ' : '❌ FALHA'} (${p99Recup.toFixed(1)}ms)     ║`);
  console.log('╚══════════════════════════════════════════════════╝\n');

  return { stdout: '' };
}
