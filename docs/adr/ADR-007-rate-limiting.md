# ADR-007 — Rate Limiting Nativo do ASP.NET Core 10

| Campo | Valor |
|-------|-------|
| **ID** | ADR-007 |
| **Título** | Rate Limiting com Fixed Window via ASP.NET Core 10 nativo |
| **Status** | ✅ Aprovado |
| **Data** | Junho 2026 |

---

## Contexto

O requisito não funcional RNF-02 exige que o serviço de Consolidado suporte **50 req/s com máx. 5% de perda**. Para proteger ambos os serviços de abusos, picos repentinos e ataques de negação de serviço, é necessário um mecanismo de throttling.

Adicionalmente, o diagrama C4 original incluía um `RateLimitMiddleware` customizado, o que foi considerado na análise de conformidade da arquitetura.

---

## Decisão

Utilizar o **Rate Limiting nativo do ASP.NET Core 10** (`Microsoft.AspNetCore.RateLimiting`), disponível a partir do .NET 7, com política **Fixed Window**:

- `PermitLimit`: 100 requisições por janela
- `Window`: 1 segundo
- `QueueLimit`: 10 (buffer para picos momentâneos)
- `QueueProcessingOrder`: OldestFirst
- Status de rejeição: `429 Too Many Requests`
- Aplicado globalmente via `.RequireRateLimiting("api")` em `MapControllers()`

---

## Alternativas Consideradas

### Middleware Customizado (`RateLimitMiddleware`) ❌
- ✅ Controle total sobre a lógica de throttling
- ❌ Requer implementação, testes e manutenção de código customizado
- ❌ Sem benefício real sobre a implementação nativa do .NET 10
- ❌ Mais propenso a bugs em cenários concorrentes (thread-safety)

### API Gateway (Nginx / Ocelot) como único ponto de rate limiting ❌
- ✅ Centraliza o throttling antes de chegar às APIs
- ❌ Depende de configuração externa; se o gateway falhar ou for bypassado, as APIs ficam desprotegidas
- ❌ Não oferece rate limiting por rota/política no nível de aplicação
- **Decisão:** o gateway pode ter rate limiting adicional, mas as APIs devem se auto-proteger (defesa em profundidade)

### Polly Rate Limiter ❌
- ✅ Já é uma dependência do projeto (Polly 8)
- ❌ Polly rate limiter é client-side (protege chamadas *de saída*), não server-side
- ❌ Não adequado para throttling de requisições HTTP *entrantes*

### ASP.NET Core 10 Rate Limiting Nativo ✅ **ESCOLHIDA**
- ✅ Zero dependência adicional (built-in no .NET 10)
- ✅ Thread-safe, performático (usa `System.Threading.RateLimiting`)
- ✅ Suporte a múltiplas políticas (Fixed Window, Sliding Window, Token Bucket, Concurrency)
- ✅ Integrado ao middleware pipeline padrão
- ✅ Elimina a necessidade de middleware customizado

---

## Trade-off Decisório

| Trade-off | Impacto | Por que foi aceito |
|-----------|---------|---------------------|
| **Fixed Window pode ter burst no início de cada janela** | Baixo | Para 100 req/s em janela de 1s, o comportamento é aceitável. Uma política Token Bucket resolveria isso mas aumentaria a complexidade sem justificativa para o volume atual. |
| **Rate limiting no nível de API, não de IP** | Médio | Para um sistema B2B com autenticação por API Key, o throttling por instância de API é mais relevante que por IP. Rate limiting por IP pode ser adicionado no gateway (Nginx). |
| **Sem rate limiting persistido entre réplicas** | Médio | Em escalonamento horizontal, cada réplica mantém sua própria janela. Para contornar, um Redis-backed rate limiter pode ser adicionado futuramente. Para o MVP com uma réplica, é adequado. |

**O trade-off que definiu a escolha:** o middleware customizado originalmente previsto no diagrama C4 não adiciona valor sobre a implementação nativa do ASP.NET Core 10, que é mais robusta, testada e sem custo de manutenção.

---

## Consequências

**Positivas:**
- Proteção automática contra abusos e picos em ambas as APIs
- Retorno semântico correto (`429 Too Many Requests`) para clientes
- Zero código customizado para manter

**Negativas:**
- Estado do rate limiter não é compartilhado entre réplicas (limitação do MVP)
- Fixed Window pode permitir burst de 2× o limit no boundary da janela

---

## Histórico de Revisões

| Versão | Data | Autor | Descrição |
|--------|------|-------|-----------|
| 1.0 | Junho 2026 | Arquiteto de Soluções | Decisão inicial — substituição de middleware customizado pelo rate limiter nativo do ASP.NET Core 10 |