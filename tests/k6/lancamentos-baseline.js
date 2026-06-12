/**
 * k6 Baseline — Serviço de Lançamentos
 *
 * Objetivo: verificar se o sistema atende o requisito mínimo de pico.
 * Cenário de referência: ~50 req/s por 5 minutos (dia de alto volume).
 *
 * Pré-requisitos:
 *   - Docker Compose UP: docker compose up -d
 *   - k6 instalado: https://k6.io/docs/get-started/installation/
 *
 * Execução:
 *   k6 run tests/k6/lancamentos-baseline.js
 *
 * Para gerar relatório HTML:
 *   k6 run --out json=tests/k6/results/lancamentos-baseline.json tests/k6/lancamentos-baseline.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// ─────────────────────────────────────────────
// Métricas customizadas
// ─────────────────────────────────────────────
const errorRate = new Rate('erros');
const latenciaCriar = new Trend('latencia_criar_lancamento');
const latenciaListar = new Trend('latencia_listar_lancamentos');
const criadosComSucesso = new Counter('lancamentos_criados');

// ─────────────────────────────────────────────
// Configuração do cenário
// ─────────────────────────────────────────────
export const options = {
  scenarios: {
    // Rampa de subida → pico → descida
    baseline: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '1m', target: 20 },   // aquecimento
        { duration: '3m', target: 50 },   // pico: ~50 req/s
        { duration: '1m', target: 0 },    // descida
      ],
    },
  },

  // ── Thresholds mínimos de aceitação ──────────
  thresholds: {
    // 95% das requisições de criação em < 500ms
    latencia_criar_lancamento: ['p(95)<500'],
    // 95% das listagens em < 200ms
    latencia_listar_lancamentos: ['p(95)<200'],
    // Taxa de erro < 1%
    erros: ['rate<0.01'],
    // HTTP failures globais < 1%
    http_req_failed: ['rate<0.01'],
  },
};

// ─────────────────────────────────────────────
// Configuração — ajuste conforme ambiente
// ─────────────────────────────────────────────
const BASE_URL = __ENV.LANCAMENTOS_URL || 'http://localhost:8080';
const API_KEY  = __ENV.API_KEY          || 'TROQUE-ESTA-CHAVE-EM-PRODUCAO';

const HEADERS = {
  'Content-Type': 'application/json',
  'X-Api-Key': API_KEY,
};

// ─────────────────────────────────────────────
// Cenários de teste
// ─────────────────────────────────────────────
export default function () {
  const idempotencyKey = generateUUID();
  const data = randomDate();

  // 1. Criar lançamento (write path)
  const payload = JSON.stringify({
    tipo: Math.random() > 0.4 ? 'Credito' : 'Debito',
    valor: +(Math.random() * 9900 + 100).toFixed(2),
    descricao: `Lançamento de teste k6 — carga baseline`,
    data: data,
  });

  const resCreate = http.post(
    `${BASE_URL}/api/lancamentos`,
    payload,
    {
      headers: { ...HEADERS, 'Idempotency-Key': idempotencyKey },
    }
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

  // Pequena pausa entre write e read (simula comportamento real)
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

// ─────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────
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