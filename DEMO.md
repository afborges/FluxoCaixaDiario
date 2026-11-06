# 🎬 DEMO — Fluxo de Caixa Diário

Guia passo-a-passo para subir o ambiente, criar lançamentos e consultar o consolidado.

---

## Pré-requisitos

| Requisito | Versão mínima |
|-----------|--------------|
| Docker Desktop | 4.x |
| .NET SDK | 10.0 |
| PowerShell / bash | qualquer |

---

## 1. Subir o ambiente completo

```bash
# Na raiz do repositório
docker compose up -d --build
```

Aguarde todos os containers ficarem `healthy` (~60 s):

```bash
docker compose ps
```

Serviços disponíveis após o startup:

| Serviço | URL |
|---------|-----|
| Lancamentos API | http://localhost:5010 |
| Consolidado API | http://localhost:5020 |
| Swagger — Lancamentos | http://localhost:5010/swagger |
| Swagger — Consolidado | http://localhost:5020/swagger |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |
| Seq (logs) | http://localhost:8088 |
| Grafana | http://localhost:3000 (admin/admin) |
| Prometheus | http://localhost:9090 |

> **API Key padrão:** `TROQUE-ESTA-CHAVE-EM-PRODUCAO`  
> Todas as chamadas precisam do header `X-Api-Key`.

---

## 2. Criar lançamentos

### 2.1 Crédito (venda)

```powershell
$hoje = (Get-Date).ToString("yyyy-MM-dd")
$headers = @{ "X-Api-Key" = "TROQUE-ESTA-CHAVE-EM-PRODUCAO"; "Content-Type" = "application/json" }

Invoke-RestMethod -Uri "http://localhost:5010/api/lancamentos" -Method POST -Headers $headers -Body (@{
	tipo      = "Credito"
	valor     = 1500.00
	descricao = "Venda de produto A"
	data      = $hoje
} | ConvertTo-Json)
```

### 2.2 Débito (pagamento)

```powershell
Invoke-RestMethod -Uri "http://localhost:5010/api/lancamentos" -Method POST -Headers $headers -Body (@{
	tipo      = "Debito"
	valor     = 300.00
	descricao = "Pagamento de fornecedor"
	data      = $hoje
} | ConvertTo-Json)
```

### 2.3 Segundo crédito

```powershell
Invoke-RestMethod -Uri "http://localhost:5010/api/lancamentos" -Method POST -Headers $headers -Body (@{
	tipo      = "Credito"
	valor     = 750.00
	descricao = "Serviço prestado"
	data      = $hoje
} | ConvertTo-Json)
```

> **Resultado esperado:** saldo do dia = 1500 + 750 − 300 = **R$ 1.950,00**

---

## 3. Listar lançamentos

```powershell
# Todos os lançamentos do dia
Invoke-RestMethod -Uri "http://localhost:5010/api/lancamentos?dataInicio=$hoje&dataFim=$hoje" `
	-Headers $headers | ConvertTo-Json -Depth 5
```

---

## 4. Obter lançamento por ID

```powershell
# Substitua pelo ID retornado no POST
$id = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"

Invoke-RestMethod -Uri "http://localhost:5010/api/lancamentos/$id" `
	-Headers $headers | ConvertTo-Json
```

---

## 5. Cancelar um lançamento

```powershell
Invoke-RestMethod -Uri "http://localhost:5010/api/lancamentos/$id" `
	-Method DELETE -Headers $headers
# HTTP 204 No Content
```

---

## 6. Consultar o consolidado diário

> O consolidado é atualizado pelo serviço Consolidado após consumir os eventos do RabbitMQ.  
> Aguarde ~2–5 s após os lançamentos antes de consultar.

```powershell
# Saldo consolidado de hoje
Invoke-RestMethod -Uri "http://localhost:5020/api/consolidado/$hoje" `
	-Headers $headers | ConvertTo-Json
```

Resposta esperada:

```json
{
  "data": "2024-01-15",
  "totalCreditos": 2250.00,
  "totalDebitos": 300.00,
  "saldoFinal": 1950.00,
  "quantidadeLancamentos": 3,
  "ultimaAtualizacao": "2024-01-15T14:30:00Z"
}
```

---

## 7. Consultar histórico de saldos

```powershell
$inicio = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd")

Invoke-RestMethod -Uri "http://localhost:5020/api/consolidado?dataInicio=$inicio&dataFim=$hoje" `
	-Headers $headers | ConvertTo-Json -Depth 4
```

---

## 8. Executar os testes

### Testes unitários

```bash
dotnet test tests/FluxoCaixaDiario.Lancamentos.UnitTests --logger "console;verbosity=normal"
dotnet test tests/FluxoCaixaDiario.Consolidado.UnitTests  --logger "console;verbosity=normal"
```

### Testes de integração (requer Docker)

```bash
dotnet test tests/FluxoCaixaDiario.Lancamentos.IntegrationTests --logger "console;verbosity=normal"
dotnet test tests/FluxoCaixaDiario.Consolidado.IntegrationTests  --logger "console;verbosity=normal"
```

### Todos os testes com cobertura

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

---

## 9. Verificar health checks

```powershell
Invoke-RestMethod "http://localhost:5010/health" | ConvertTo-Json
Invoke-RestMethod "http://localhost:5020/health" | ConvertTo-Json
```

---

## 10. Derrubar o ambiente

```bash
docker compose down -v
```

> `-v` remove os volumes (dados do SQL Server, Redis, RabbitMQ e Seq).

---

## Fluxo de dados (resumo)

```
[Cliente] ──POST /api/lancamentos──▶ [Lancamentos API]
										   │
									 Outbox (DB)
										   │
									 OutboxProcessor
										   │
									  RabbitMQ
										   │
								  [Consolidado API]
								   (Consumer MassTransit)
										   │
									 ConsolidadoDB
										   │
							  ◀──GET /api/consolidado──[Cliente]
```
