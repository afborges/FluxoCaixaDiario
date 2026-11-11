/**
 * k6 Soak Test — Serviço de Consolidado Diário
 *
 * Objetivo: verificar ESTABILIDADE PROLONGADA sob carga de leitura sustentada.
 * Detecta vazamentos de memória, saturação de cache Redis, esgotamento de
 * pool de conexões e degradação progressiva ao longo do tempo.
 *
 * Carga: 100 req/s (mesmo rate do baseline) por período configurável.
 *
 * Pré-requisitos:
 *   - Docker Compose UP: docker compose up -d
 *   - k6 instalado: https://k6.io/docs/get-started/installation/
 *   - Ao menos 30 dias de dados (execute lancamentos-soak.js ou lancamentos-baseline.js antes)
 *
 * Execução:
 *   k6 run tests/k6/consolidado-soak.js
 *
 * Para aumentar a duração (produção recomenda ≥ 4h):
 *   k6 run -e SOAK_DURATION=4h tests/k6/consolidado-soak.js
 *
 * Para gerar relatório JSON:
 *   k6 run --out json=tests/k6/results/consolidado-soak.json tests/k6/consolidado-soak.js
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

// ─────────────────────────────────────────────────────────────────────────────
// Configuração do cenário
// ─────────────────────────────────────────────────────────────────────────────
const SOAK_DURATION = __ENV.SOAK_DURATION || '30m';
const SOAK_RATE     = parseInt(__ENV.SOAK_RATE || '100'); // req/s

export const options = {
  scenarios: {
    // Fase de aquecimento com ramping para estabilizar o cache antes do soak
    aquecimento: {
      executor: 'ramping-arrival-rate',
      startRate: 10,
      timeUnit: '1s',
      preAllocatedVUs: 20,
      maxVUs: 100,
      stages: [
        { duration: '5m', target: SOAK_RATE }, // rampa gradual até o rate-alvo
      ],
      exec: 'cenarioPadrao',
    },

    // Soak: constant-arrival-rate para carga sustentada precisa
    soak: {
      executor: 'constant-arrival-rate',
      rate: SOAK_RATE,
      timeUnit: '1s',
      duration: SOAK_DURATION,
      preAllocatedVUs: 50,
      maxVUs: 200,
      startTime: '5m', // inicia após o aquecimento
      exec: 'cenarioPadrao',
    },

    // Desaquecimento para observar comportamento de queda gradual
    desaquecimento: {
      executor: 'ramping-arrival-rate',
      startRate: SOAK_RATE,
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 200,
      stages: [
        { duration: '5m', target: 0 },
      ],
      startTime: `5m${SOAK_DURATION}`, // após aquecimento + soak
      exec: 'cenarioPadrao',
    },
  },

  // ── Thresholds de aceitação ──────────────────────────────────────────────
  thresholds: {
    // RNF-05: mesmos critérios do baseline — devem manter-se ao longo de TODA a duração.
    // Degradação progressiva de p95 indica saturação de cache ou vazamento de recurso.
    latencia_consolidado_dia: ['p(95)<100', 'p(99)<200'],
    latencia_historico:        ['p(95)<300'],
    // RNF-03: taxa de perda < 0.5% (leitura deve ser mais estável que escrita)
    erros:           ['rate<0.005'],
    http_req_failed: ['rate<0.005'],
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
// Função principal
// ─────────────────────────────────────────────────────────────────────────────
export function cenarioPadrao() {
  const usarHoje = Math.random() < 0.7; // 70% consultas do dia corrente (mais cache hit)
  const data = usarHoje
    ? DATAS_RECENTES[0]
    : DATAS_RECENTES[Math.floor(Math.random() * DATAS_RECENTES.length)];

  // 1. Consulta de consolidado do dia (principal — read path com cache)
  const t0 = Date.now();
  const resConsolidado = http.get(
    `${BASE_URL}/api/consolidado/${data}`,
    { headers: HEADERS }
  );
  const duracaoConsolidado = Date.now() - t0;

  latenciaConsolidado.add(resConsolidado.timings.duration);

  const consolidadoOk = check(resConsolidado, {
    'GET /consolidado/{data} status 200 ou 404': (r) => r.status === 200 || r.status === 404,
    'GET /consolidado/{data} resposta em < 500ms': (r) => r.timings.duration < 500,
  });

  // Detectar se provavelmente foi cache hit (< 20ms indica Redis)
  if (resConsolidado.status === 200 && duracaoConsolidado < 20) {
    latenciaCacheHit.add(duracaoConsolidado);
  }

  errorRate.add(!consolidadoOk);

  // 2. Consulta de histórico (30% das iterações)
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
// Resumo final customizado
// ─────────────────────────────────────────────────────────────────────────────
export function handleSummary(data) {
  const p95       = data.metrics['latencia_consolidado_dia']?.values['p(95)'] ?? 0;
  const p99       = data.metrics['latencia_consolidado_dia']?.values['p(99)'] ?? 0;
  const p95Hist   = data.metrics['latencia_historico']?.values['p(95)']       ?? 0;
  const cacheHits = data.metrics['latencia_cache_hit']?.values.count          ?? 0;
  const totalCons = data.metrics['latencia_consolidado_dia']?.values.count    ?? 1;
  const taxaErro  = (data.metrics['erros']?.values.rate ?? 0) * 100;
  const totalReqs = data.metrics['http_reqs']?.values.count                   ?? 0;

  console.log('\n╔══════════════════════════════════════════════════╗');
  console.log('║   RESULTADO — Consolidado Soak Test              ║');
  console.log('╠══════════════════════════════════════════════════╣');
  console.log(`║  Total de requisições    : ${String(totalReqs).padStart(10)}              ║`);
  console.log(`║  p95 consolidado         : ${p95.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p99 consolidado         : ${p99.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p95 histórico           : ${p95Hist.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  Estimativa cache hits   : ${String(cacheHits).padStart(10)}              ║`);
  console.log(`║  Taxa de cache hit       : ${((cacheHits / totalCons) * 100).toFixed(1).padStart(7)}%             ║`);
  console.log(`║  Taxa de erro            : ${taxaErro.toFixed(2).padStart(7)}%             ║`);
  console.log(`║  p95 < 100ms             : ${p95 < 100 ? '✅ OK   ' : '❌ FALHA'} (${p95.toFixed(1)}ms)     ║`);
  console.log(`║  p99 < 200ms             : ${p99 < 200 ? '✅ OK   ' : '❌ FALHA'} (${p99.toFixed(1)}ms)     ║`);
  console.log('╚══════════════════════════════════════════════════╝\n');

  return { stdout: '' };
}
