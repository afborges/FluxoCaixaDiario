/**
 * k6 Spike Test — Serviço de Lançamentos
 *
 * Objetivo: verificar comportamento do sistema frente a picos SÚBITOS de carga
 * e validar RECUPERAÇÃO após o pico (latência e taxa de erro retornam ao baseline).
 *
 * Cenário: simula abertura de mercado / fechamento de folha (carga 6× o baseline).
 *
 * Fases:
 *   1. aquecimento  (0s)       : 30s → 20 VUs  (carga normal de referência)
 *   2. spike_subida (30s)      : 10s → 300 VUs (pico súbito ×6)
 *   3. spike_pico   (40s)      : 1m  → 300 VUs (sustentação do pico)
 *   4. spike_descida (1m40s)   : 10s → 20 VUs  (queda brusca)
 *   5. recuperacao  (1m50s)    : 3m  → 20 VUs  (janela de validação de recuperação)
 *
 * A fase "recuperacao" usa exec separado para registrar a métrica
 * `latencia_recuperacao`, que tem threshold próprio igual ao baseline.
 * Isso garante que o sistema voltou ao nível normal após o pico.
 *
 * Pré-requisitos:
 *   - Docker Compose UP: docker compose up -d
 *   - k6 instalado: https://k6.io/docs/get-started/installation/
 *
 * Execução:
 *   k6 run tests/k6/lancamentos-spike.js
 *
 * Para aumentar a intensidade do pico:
 *   k6 run -e SPIKE_VUS=500 tests/k6/lancamentos-spike.js
 *
 * Para gerar relatório JSON:
 *   k6 run --out json=tests/k6/results/lancamentos-spike.json tests/k6/lancamentos-spike.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// ─────────────────────────────────────────────────────────────────────────────
// Métricas customizadas
// ─────────────────────────────────────────────────────────────────────────────
const errorRate           = new Rate('erros');
const latenciaCriar       = new Trend('latencia_criar_lancamento');
const latenciaListar      = new Trend('latencia_listar_lancamentos');
const latenciaRecuperacao = new Trend('latencia_recuperacao');  // fase pós-pico
const criadosComSucesso   = new Counter('lancamentos_criados');

// ─────────────────────────────────────────────────────────────────────────────
// Configuração do cenário
// ─────────────────────────────────────────────────────────────────────────────
const SPIKE_VUS = parseInt(__ENV.SPIKE_VUS || '300');

export const options = {
  scenarios: {
    // Fases 1–4: aquecimento → pico → queda (ramping-vus)
    spike: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '30s', target: 20         }, // 1. aquecimento
        { duration: '10s', target: SPIKE_VUS  }, // 2. pico súbito
        { duration: '1m',  target: SPIKE_VUS  }, // 3. sustentação
        { duration: '10s', target: 20         }, // 4. queda brusca
      ],
      exec: 'cenarioPadrao',
    },

    // Fase 5: janela de recuperação com métrica separada
    // startTime = 30s + 10s + 60s + 10s = 110s após o início
    recuperacao: {
      executor: 'constant-vus',
      vus: 20,
      duration: '3m',
      startTime: '1m50s',
      exec: 'cenarioRecuperacao',
    },
  },

  // ── Thresholds de aceitação ──────────────────────────────────────────────
  thresholds: {
    // Durante o pico: tolerância expandida (2× o baseline)
    latencia_criar_lancamento:   ['p(99)<1000'],
    latencia_listar_lancamentos: ['p(95)<500'],
    // RNF-03: até 5% de erros durante o pico (burst traffic)
    erros:           ['rate<0.05'],
    http_req_failed: ['rate<0.05'],
    // Validação de RECUPERAÇÃO: após o pico, latência deve retornar ao baseline
    // p95 < 300ms e p99 < 500ms (mesmos critérios do RNF-04)
    latencia_recuperacao: ['p(95)<300', 'p(99)<500'],
  },
};

// ─────────────────────────────────────────────────────────────────────────────
// Configuração — ajuste conforme ambiente
// ─────────────────────────────────────────────────────────────────────────────
const BASE_URL = __ENV.LANCAMENTOS_URL || 'http://localhost:8080';
const API_KEY  = __ENV.API_KEY         || 'TROQUE-ESTA-CHAVE-EM-PRODUCAO';

const HEADERS = {
  'Content-Type': 'application/json',
  'X-Api-Key': API_KEY,
};

// ─────────────────────────────────────────────────────────────────────────────
// Cenário padrão (fases 1–4: aquecimento + pico + queda)
// ─────────────────────────────────────────────────────────────────────────────
export function cenarioPadrao() {
  const idempotencyKey = generateUUID();
  const data = randomDate();

  const payload = JSON.stringify({
    tipo: Math.random() > 0.4 ? 'Credito' : 'Debito',
    valor: +(Math.random() * 9900 + 100).toFixed(2),
    descricao: `Lançamento spike test — ${new Date().toISOString()}`,
    data: data,
  });

  const resCreate = http.post(
    `${BASE_URL}/api/lancamentos`,
    payload,
    { headers: { ...HEADERS, 'Idempotency-Key': idempotencyKey } }
  );

  latenciaCriar.add(resCreate.timings.duration);

  const criadoOk = check(resCreate, {
    'POST /lancamentos status 201 ou 200': (r) => r.status === 201 || r.status === 200,
    'POST /lancamentos tem campo id': (r) => {
      try { return JSON.parse(r.body).id !== undefined; } catch { return false; }
    },
  });

  errorRate.add(!criadoOk);
  if (criadoOk) criadosComSucesso.add(1);

  sleep(0.05);

  const resListar = http.get(
    `${BASE_URL}/api/lancamentos?dataInicio=${data}&dataFim=${data}&pagina=1&tamanhoPagina=20`,
    { headers: HEADERS }
  );

  latenciaListar.add(resListar.timings.duration);

  const listarOk = check(resListar, {
    'GET /lancamentos status 200': (r) => r.status === 200,
  });

  errorRate.add(!listarOk);

  sleep(0.05);
}

// ─────────────────────────────────────────────────────────────────────────────
// Cenário de recuperação (fase 5: valida retorno ao baseline pós-pico)
// Registra latência em métrica separada `latencia_recuperacao`.
// ─────────────────────────────────────────────────────────────────────────────
export function cenarioRecuperacao() {
  const idempotencyKey = generateUUID();
  const data = randomDate();

  const payload = JSON.stringify({
    tipo: Math.random() > 0.4 ? 'Credito' : 'Debito',
    valor: +(Math.random() * 9900 + 100).toFixed(2),
    descricao: `Lançamento recuperação pós-pico — ${new Date().toISOString()}`,
    data: data,
  });

  const resCreate = http.post(
    `${BASE_URL}/api/lancamentos`,
    payload,
    { headers: { ...HEADERS, 'Idempotency-Key': idempotencyKey } }
  );

  // Registrar tanto na métrica geral quanto na métrica de recuperação
  latenciaCriar.add(resCreate.timings.duration);
  latenciaRecuperacao.add(resCreate.timings.duration);

  const criadoOk = check(resCreate, {
    '[recuperacao] POST /lancamentos status 201 ou 200': (r) => r.status === 201 || r.status === 200,
    '[recuperacao] latencia < 500ms': (r) => r.timings.duration < 500,
  });

  errorRate.add(!criadoOk);
  if (criadoOk) criadosComSucesso.add(1);

  sleep(0.1);

  const resListar = http.get(
    `${BASE_URL}/api/lancamentos?dataInicio=${data}&dataFim=${data}&pagina=1&tamanhoPagina=20`,
    { headers: HEADERS }
  );

  latenciaListar.add(resListar.timings.duration);

  const listarOk = check(resListar, {
    '[recuperacao] GET /lancamentos status 200': (r) => r.status === 200,
  });

  errorRate.add(!listarOk);

  sleep(0.3);
}

// ─────────────────────────────────────────────────────────────────────────────
// Resumo final customizado
// ─────────────────────────────────────────────────────────────────────────────
export function handleSummary(data) {
  const p95Criar  = data.metrics['latencia_criar_lancamento']?.values['p(95)']  ?? 0;
  const p99Criar  = data.metrics['latencia_criar_lancamento']?.values['p(99)']  ?? 0;
  const p95Recup  = data.metrics['latencia_recuperacao']?.values['p(95)']       ?? 0;
  const p99Recup  = data.metrics['latencia_recuperacao']?.values['p(99)']       ?? 0;
  const taxaErro  = (data.metrics['erros']?.values.rate ?? 0) * 100;
  const totalReqs = data.metrics['http_reqs']?.values.count ?? 0;

  const recuperouP95 = p95Recup > 0 && p95Recup < 300;
  const recuperouP99 = p99Recup > 0 && p99Recup < 500;

  console.log('\n╔══════════════════════════════════════════════════╗');
  console.log('║   RESULTADO — Lançamentos Spike Test             ║');
  console.log('╠══════════════════════════════════════════════════╣');
  console.log(`║  Total de requisições    : ${String(totalReqs).padStart(10)}              ║`);
  console.log(`║  p95 criar (geral)       : ${p95Criar.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p99 criar (geral)       : ${p99Criar.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p95 recuperação pós-pico: ${p95Recup.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p99 recuperação pós-pico: ${p99Recup.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  Taxa de erro (geral)    : ${taxaErro.toFixed(2).padStart(7)}%             ║`);
  console.log(`║  Recuperou p95 < 300ms   : ${recuperouP95 ? '✅ OK   ' : '❌ FALHA'} (${p95Recup.toFixed(1)}ms)     ║`);
  console.log(`║  Recuperou p99 < 500ms   : ${recuperouP99 ? '✅ OK   ' : '❌ FALHA'} (${p99Recup.toFixed(1)}ms)     ║`);
  console.log('╚══════════════════════════════════════════════════╝\n');

  return { stdout: '' };
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────
function generateUUID() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
}

function randomDate() {
  const today = new Date();
  today.setDate(today.getDate() - Math.floor(Math.random() * 30));
  return today.toISOString().split('T')[0];
}
