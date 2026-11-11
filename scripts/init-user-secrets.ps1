<#
.SYNOPSIS
	Inicializa o dotnet user-secrets para desenvolvimento local.

.DESCRIPTION
	Define os segredos necessários para executar as APIs fora do Docker
	(dotnet run / Visual Studio). Os valores padrão espelham o ambiente Docker
	definido em .env.example.

	Execute uma única vez após clonar o repositório:
		.\scripts\init-user-secrets.ps1

	Para sobrescrever valores existentes, passe o switch -Force:
		.\scripts\init-user-secrets.ps1 -Force

.NOTES
	Requer o .NET SDK instalado (dotnet user-secrets).
	Os segredos ficam em %APPDATA%\Microsoft\UserSecrets\ e NUNCA são commitados.
#>
param(
	[switch]$Force,

	# Personalize os valores aqui ou passe-os como parâmetros
	[string]$SqlPassword      = "FluxoCaixa@2024",
	[string]$RabbitPassword   = "guest",
	[string]$JwtSecret        = "FluxoCaixaDiario-JWT-Secret-Dev-Only-32chars!!",
	[string]$LancamentosApiKey = "minha-api-key-dev",
	[string]$ConsolidadoApiKey = "minha-api-key-dev"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

function Set-Secret {
	param([string]$Project, [string]$Key, [string]$Value)
	$result = dotnet user-secrets set $Key $Value --project $Project 2>&1
	if ($LASTEXITCODE -ne 0) { throw "Falha ao definir '$Key': $result" }
	Write-Host "  [OK] $Key" -ForegroundColor Green
}

# --- Lancamentos API ---
$lancamentosProj = Join-Path $root "src\Lancamentos\FluxoCaixaDiario.Lancamentos.API\FluxoCaixaDiario.Lancamentos.API.csproj"
Write-Host "`nConfigurando segredos: Lancamentos API" -ForegroundColor Cyan
Set-Secret $lancamentosProj "ConnectionStrings:LancamentosDb" "Server=localhost,1433;Database=LancamentosDB;User Id=sa;Password=$SqlPassword;TrustServerCertificate=True;"
Set-Secret $lancamentosProj "RabbitMQ:Password"              $RabbitPassword
Set-Secret $lancamentosProj "RabbitMQ:ConnectionString"      "amqp://guest:$RabbitPassword@localhost:5672/"
Set-Secret $lancamentosProj "ApiKey:Key"                     $LancamentosApiKey
Set-Secret $lancamentosProj "Jwt:Secret"                     $JwtSecret

# --- Consolidado API ---
$consolidadoProj = Join-Path $root "src\Consolidado\FluxoCaixaDiario.Consolidado.API\FluxoCaixaDiario.Consolidado.API.csproj"
Write-Host "`nConfigurando segredos: Consolidado API" -ForegroundColor Cyan
Set-Secret $consolidadoProj "ConnectionStrings:ConsolidadoDb" "Server=localhost,1433;Database=ConsolidadoDB;User Id=sa;Password=$SqlPassword;TrustServerCertificate=True;"
Set-Secret $consolidadoProj "ConnectionStrings:Redis"         "localhost:6379"
Set-Secret $consolidadoProj "RabbitMQ:Password"               $RabbitPassword
Set-Secret $consolidadoProj "RabbitMQ:ConnectionString"       "amqp://guest:$RabbitPassword@localhost:5672/"
Set-Secret $consolidadoProj "ApiKey:Key"                      $ConsolidadoApiKey
Set-Secret $consolidadoProj "Jwt:Secret"                      $JwtSecret

Write-Host "`nSegredos configurados com sucesso." -ForegroundColor Green
Write-Host "Execute 'dotnet run' nos projetos de API para iniciar sem Docker." -ForegroundColor Yellow
Write-Host "`nPara rodar migrations do EF Core, defina as variáveis de ambiente primeiro:" -ForegroundColor Yellow
Write-Host "  `$env:ConnectionStrings__LancamentosDb = 'Server=localhost,1433;Database=LancamentosDB;User Id=sa;Password=$SqlPassword;TrustServerCertificate=True;'" -ForegroundColor DarkGray
Write-Host "  `$env:ConnectionStrings__ConsolidadoDb  = 'Server=localhost,1433;Database=ConsolidadoDB;User Id=sa;Password=$SqlPassword;TrustServerCertificate=True;'" -ForegroundColor DarkGray
