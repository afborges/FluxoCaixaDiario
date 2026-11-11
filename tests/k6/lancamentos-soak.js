/**
 * k6 Soak Test — Serviço de Lançamentos
 *
 * Objetivo: verificar ESTABILIDADE PROLONGADA sob carga sustentada.
 * Detecta vazamentos de memória, esgotamento de pool de conexões e
 * degradação progressiva de latência ao longo do tempo.
 *
 * Carga: 80% do baseline (~40 VUs) por período configurável.
 *
 * Pré-requisitos:
 *   - Docker Compose UP: docker compose up -d
 *   - k6 instalado: https://k6.io/docs/get-started/installation/
 *
 * Execução:
 *   k6 run tests/k6/lancamentos-soak.js
 *
 * Para aumentar a duração (produção recomenda ≥ 4h):
 *   k6 run -e SOAK_DURATION=4h tests/k6/lancamentos-soak.js
 *
 * Para gerar relatório JSON:
 *   k6 run --out json=tests/k6/results/lancamentos-soak.json tests/k6/lancamentos-soak.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// ─────────────────────────────────────────────────────────────────────────────
// Métricas customizadas
// ─────────────────────────────────────────────────────────────────────────────
const errorRate         = new Rate('erros');
const latenciaCriar     = new Trend('latencia_criar_lancamento');
const latenciaListar    = new Trend('latencia_listar_lancamentos');
const criadosComSucesso = new Counter('lancamentos_criados');

// ─────────────────────────────────────────────────────────────────────────────
// Configuração do cenário
// ─────────────────────────────────────────────────────────────────────────────
const SOAK_DURATION = __ENV.SOAK_DURATION || '30m';
const SOAK_VUS      = parseInt(__ENV.SOAK_VUS || '40');

export const options = {
  scenarios: {
    soak: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '5m',           target: SOAK_VUS }, // aquecimento gradual
        { duration: SOAK_DURATION,  target: SOAK_VUS }, // carga sustentada (soak)
        { duration: '5m',           target: 0         }, // desaquecimento
      ],
    },
  },

  // ── Thresholds de aceitação ──────────────────────────────────────────────
  thresholds: {
    // Mesmos critérios do baseline — devem manter-se ao longo de TODA a duração.
    // Degradação progressiva indica vazamento de recurso (memória, conexões, etc.).
    latencia_criar_lancamento:  ['p(95)<300', 'p(99)<500'],
    latencia_listar_lancamentos: ['p(95)<200'],
    // RNF-03: taxa de perda < 1% (meta operacional contínua)
    erros:            ['rate<0.01'],
    http_req_failed:  ['rate<0.01'],
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
// Função principal
// ─────────────────────────────────────────────────────────────────────────────
export default function () {
  const idempotencyKey = generateUUID();
  const data = randomDate();

  // 1. Criar lançamento (write path)
  const payload = JSON.stringify({
    tipo: Math.random() > 0.4 ? 'Credito' : 'Debito',
    valor: +(Math.random() * 9900 + 100).toFixed(2),
    descricao: `Lançamento soak test — ${new Date().toISOString()}`,
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

  sleep(0.1);

  // 2. Listar lançamentos do dia (read path)
  const resListar = http.get(
    `${BASE_URL}/api/lancamentos?dataInicio=${data}&dataFim=${data}&pagina=1&tamanhoPagina=20`,
    { headers: HEADERS }
  );

  latenciaListar.add(resListar.timings.duration);

  const listarOk = check(resListar, {
    'GET /lancamentos status 200': (r) => r.status === 200,
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
  const p95Listar = data.metrics['latencia_listar_lancamentos']?.values['p(95)'] ?? 0;
  const taxaErro  = (data.metrics['erros']?.values.rate ?? 0) * 100;
  const totalReqs = data.metrics['http_reqs']?.values.count ?? 0;
  const criados   = data.metrics['lancamentos_criados']?.values.count ?? 0;

  console.log('\n╔══════════════════════════════════════════════════╗');
  console.log('║   RESULTADO — Lançamentos Soak Test              ║');
  console.log('╠══════════════════════════════════════════════════╣');
  console.log(`║  Total de requisições    : ${String(totalReqs).padStart(10)}              ║`);
  console.log(`║  Lançamentos criados     : ${String(criados).padStart(10)}              ║`);
  console.log(`║  p95 criar lançamento    : ${p95Criar.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p99 criar lançamento    : ${p99Criar.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  p95 listar lançamentos  : ${p95Listar.toFixed(1).padStart(8)}ms            ║`);
  console.log(`║  Taxa de erro            : ${taxaErro.toFixed(2).padStart(7)}%             ║`);
  console.log(`║  p99 criar < 500ms       : ${p99Criar < 500 ? '✅ OK   ' : '❌ FALHA'} (${p99Criar.toFixed(1)}ms)     ║`);
  console.log(`║  p95 listar < 200ms      : ${p95Listar < 200 ? '✅ OK   ' : '❌ FALHA'} (${p95Listar.toFixed(1)}ms)     ║`);
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
