/**
 * k6 Baseline — Serviço de Consolidado Diário
 *
 * Objetivo: verificar se o serviço de leitura atende o pico de consultas.
 * Cenário de referência: ~100 req/s (read-heavy — múltiplos dashboards consultando
 * o consolidado do dia corrente e histórico recente).
 *
 * Pré-requisitos:
 *   - Docker Compose UP: docker compose up -d
 *   - k6 instalado: https://k6.io/docs/get-started/installation/
 *   - Ao menos 30 dias de dados (execute lancamentos-baseline.js antes)
 *
 * Execução:
 *   k6 run tests/k6/consolidado-baseline.js
 *
 * Para gerar relatório JSON:
 *   k6 run --out json=tests/k6/results/consolidado-baseline.json tests/k6/consolidado-baseline.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// ─────────────────────────────────────────────
// Métricas customizadas
// ─────────────────────────────────────────────
const errorRate = new Rate('erros');
const latenciaConsolidado = new Trend('latencia_consolidado_dia');
const latenciaHistorico = new Trend('latencia_historico');
const latenciaCacheHit = new Trend('latencia_cache_hit');

// ─────────────────────────────────────────────
// Configuração do cenário
// ─────────────────────────────────────────────
export const options = {
  scenarios: {
    // Carga sustentada simula dashboards abertos continuamente
    leitura_sustentada: {
      executor: 'constant-arrival-rate',
      rate: 100,           // 100 requisições por segundo
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },

  // ── Thresholds mínimos de aceitação ──────────
  thresholds: {
    // 95% das consultas de consolidado em < 100ms (esperamos cache hit)
    latencia_consolidado_dia: ['p(95)<100', 'p(99)<300'],
    // 95% do histórico em < 300ms
    latencia_historico: ['p(95)<300'],
    // Taxa de erro < 0.5%
    erros: ['rate<0.005'],
    http_req_failed: ['rate<0.005'],
  },
};

// ─────────────────────────────────────────────
// Configuração
// ─────────────────────────────────────────────
const BASE_URL = __ENV.CONSOLIDADO_URL || 'http://localhost:8080';
const API_KEY  = __ENV.API_KEY          || 'TROQUE-ESTA-CHAVE-EM-PRODUCAO';

const HEADERS = {
  'X-Api-Key': API_KEY,
};

// Datas de referência para simular consultas variadas
const DATAS_RECENTES = Array.from({ length: 30 }, (_, i) => {
  const d = new Date();
  d.setDate(d.getDate() - i);
  return d.toISOString().split('T')[0];
});

// ─────────────────────────────────────────────
// Cenários de teste
// ─────────────────────────────────────────────
export default function () {
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

// ─────────────────────────────────────────────
// Resumo final customizado
// ─────────────────────────────────────────────
export function handleSummary(data) {
  const p95 = data.metrics['latencia_consolidado_dia']?.values['p(95)'] ?? 0;
  const cacheHits = data.metrics['latencia_cache_hit']?.values.count ?? 0;
  const totalConsolidado = data.metrics['latencia_consolidado_dia']?.values.count ?? 1;
  const taxaErro = (data.metrics['erros']?.values.rate ?? 0) * 100;

  console.log('\n╔══════════════════════════════════════════════════╗');
  console.log('║   RESULTADO — Consolidado Baseline               ║');
  console.log('╠══════════════════════════════════════════════════╣');
  console.log(`║  p95 latência consolidado : ${p95.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  Estimativa cache hits    : ${String(cacheHits).padStart(8)} reqs          ║`);
  console.log(`║  Taxa de cache hit        : ${((cacheHits / totalConsolidado) * 100).toFixed(1).padStart(7)}%            ║`);
  console.log(`║  Taxa de erro             : ${taxaErro.toFixed(2).padStart(7)}%            ║`);
  console.log(`║  Status p95 < 100ms       : ${p95 < 100 ? '✅ OK   ' : '❌ FALHA'} (${p95.toFixed(1)}ms)     ║`);
  console.log('╚══════════════════════════════════════════════════╝\n');

  return {
    stdout: '',
  };
}