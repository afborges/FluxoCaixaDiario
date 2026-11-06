# ADR-006 — Autenticação por API Key como Baseline de Segurança

| Campo | Valor |
|-------|-------|
| **ID** | ADR-006 |
| **Título** | Autenticação por API Key (baseline MVP) com caminho de migração para OAuth2/JWT |
| **Status** | ✅ Aprovado |
| **Data** | Junho 2026 |

---

## Contexto

O requisito de segurança exige que os endpoints das APIs não fiquem completamente expostos. O modelo alvo de produção é OAuth2/OpenID Connect com um Identity Provider (Keycloak ou Azure AD B2C), conforme princípio P5 (*Segurança em Profundidade*). Porém, neste estágio do MVP:

- Não há servidor de identidade disponível na infraestrutura local
- Configurar e manter um IdP (Keycloak, Azure AD B2C) está fora do escopo do desafio atual
- A solução de segurança não pode bloquear a entrega funcional

**Restrições:**
- Deve ser implementável sem servidores externos
- Deve ser reversível/substituível quando OAuth2 for adotado
- Deve bloquear acessos não autorizados em todos os endpoints de negócio
- Deve manter `/health`, `/metrics` e `/swagger` acessíveis sem credencial (monitoramento e DX)

---

## Decisão

Implementar autenticação por **API Key via header HTTP `X-Api-Key`** como middleware ASP.NET Core em ambos os serviços (`Lancamentos.API` e `Consolidado.API`), com:

1. **Middleware `ApiKeyAuthMiddleware`** — intercepta todas as requisições antes de chegar aos controllers
2. **Configuração via `appsettings.json`** — chave substituível por variável de ambiente (`ApiKey__Key`) sem recompilação
3. **Bypass para rotas de infra** — `/health`, `/metrics`, `/swagger` não exigem autenticação
4. **Security headers** — `X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection` adicionados em todas as respostas
5. **HTTPS Redirection** — `UseHttpsRedirection()` habilitado no pipeline
6. **Swagger integrado** — campo `Authorize → ApiKey` no Swagger UI para uso durante desenvolvimento

---

## Alternativas Consideradas

### OAuth2/JWT com Keycloak ❌ (para agora)
- ✅ Padrão de mercado para autenticação em microsserviços
- ✅ Suporte a autorização granular (RBAC por scope)
- ✅ Rotação de credenciais sem alterar os serviços
- ❌ Requer servidor Keycloak rodando (adiciona dependência de infra ao Docker Compose)
- ❌ Aumenta a complexidade de configuração para demonstração do desafio
- ❌ Tempo de setup elevado para o escopo atual
- **Decisão:** planejado para Fase 2 (produção)

### Basic Authentication ❌
- ✅ Nativo no ASP.NET Core
- ❌ Credenciais em Base64 não oferecem proteção real sem TLS
- ❌ Semântica inadequada para comunicação service-to-service
- ❌ Não há gerenciamento de usuários neste escopo

### Nenhuma autenticação ❌
- ❌ Viola o princípio P5 do documento de arquitetura
- ❌ Dados financeiros ficam completamente expostos
- ❌ Não atende o requisito de segurança do desafio

### API Key via Header `X-Api-Key` ✅ **ESCOLHIDA**

**Por que API Key é adequada para este estágio?**

| Critério | Avaliação |
|----------|-----------|
| Implementação sem IdP externo | ✅ Sim — apenas configuração local |
| Bloqueia acesso não autorizado | ✅ Sim — 401 para chaves ausentes ou inválidas |
| Substituível por OAuth2 | ✅ Sim — basta remover o middleware e adicionar `AddAuthentication().AddJwtBearer()` |
| Configurável sem recompilação | ✅ Sim — via `ApiKey__Key` env var |
| Rastreabilidade de acessos inválidos | ✅ Sim — log de IP + tentativas no Serilog |
| Visível no Swagger UI | ✅ Sim — `AddSecurityDefinition("ApiKey", ...)` |
| Adequado para produção real | ⚠️ Parcial — aceitável em ambiente controlado; OAuth2 para produção |

---

## Trade-off Decisório

| Trade-off | Impacto | Por que foi aceito |
|-----------|---------|--------------------|
| **Segredo compartilhado** (todos os clientes usam a mesma chave) | Alto | Aceitável no MVP: há um único cliente (o gateway Nginx). Em produção, cada client teria sua própria chave rotacionável via secret manager. |
| **Sem identidade de usuário** — apenas autenticação de serviço | Alto | Reconhecido como limitação de escopo. A auditoria de ações individuais de usuários requer OAuth2/JWT com claims de identidade (Fase 2). |
| **Rotação de chave requer restart** (sem gerenciamento dinâmico) | Médio | Mitigado: variável de ambiente `ApiKey__Key` + rolling update zero-downtime. Em produção, integração com Azure Key Vault ou Vault resolve isso. |
| **Sem expiração automática** de credencial | Médio | Processo manual de rotação (atualizar env var + deploy). Risco gerenciável no MVP com número reduzido de ambientes. |
| **OAuth2 seria mais seguro** e correto para produção | Positivo (reconhecido) | O custo de adicionar Keycloak (container adicional + configuração complexa) excede o benefício para um desafio técnico. O path de migração está documentado e é um único commit. |

**O trade-off que definiu a escolha:** a decisão foi deliberadamente táctica — a segurança mínima necessária (bloquear acesso não autorizado) com o menor overhead de infraestrutura. O ADR documenta explicitamente que API Key **não é** a solução de produção, e o caminho de migração para OAuth2 está definido para não criar dívida técnica oculta.

---

## Consequências

### Positivas
- APIs protegidas imediatamente, sem dependência de infra adicional
- Configuração trivial via variável de ambiente em qualquer ambiente (dev, staging, prod)
- Rastreamento de tentativas de acesso inválido via Serilog
- Path de migração limpo para OAuth2: o middleware pode ser removido em um único commit

### Negativas / Riscos
- Chave é um segredo compartilhado — se vazar, todos os clientes precisam ser atualizados
- Sem suporte nativo a expiração de credencial ou rotação automática
- Não há identidade de usuário — apenas autenticação de serviço/cliente
- Em produção, a chave **deve** vir de secret manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) — nunca de arquivo versionado

### Mitigações
- Em produção: substituir o valor em `appsettings.json` por referência à variável de ambiente
- Rotação manual: atualizar a variável de ambiente e reiniciar os pods (zero-downtime com rolling update)
- Auditoria: todos os acessos inválidos são logados com IP e timestamp no Serilog/Seq

---

## Caminho de Migração para OAuth2 (Fase 2)

```
MVP (agora)           Fase 2 (produção)
──────────────        ─────────────────────────────────────
X-Api-Key header  →   Bearer JWT (OAuth2 Authorization Code + PKCE)
ApiKeyMiddleware  →   AddAuthentication().AddJwtBearer(...)
appsettings key   →   Keycloak / Azure AD B2C / AWS Cognito
Sem roles         →   Scopes: lancamentos:write, lancamentos:read, consolidado:read
```

A migração não requer mudanças nos controllers (adicionar `[Authorize]` é opcional pois o middleware já protege globalmente), apenas substituição do middleware no `Program.cs`.

---

## Histórico de Revisões

| Versão | Data | Autor | Descrição |
|--------|------|-------|-----------|
| 1.0 | Junho 2026 | Arquiteto de Soluções | Decisão inicial — API Key como baseline MVP |
| 1.1 | Junho 2026 | Arquiteto de Soluções | Adicionada seção de trade-offs; auditoria de chaves via `AuditBehavior` confirmada |

---

## Referências

- [Princípio P5 — Segurança em Profundidade](../02-arquitetura-solucao.md#2-princípios-arquiteturais)
- [Seção 6 — Arquitetura de Segurança](../02-arquitetura-solucao.md#6-arquitetura-de-segurança)
- [ADR-001 — Microsserviços](ADR-001-microservicos.md)
- [OWASP API Security Top 10](https://owasp.org/API-Security/editions/2023/en/0x11-t10/)