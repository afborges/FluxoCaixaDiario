# ADR-003 — SQL Server com Entity Framework Core por Serviço

| Campo | Valor |
|-------|-------|
| **ID** | ADR-003 |
| **Título** | SQL Server com EF Core — Database per Service Pattern |
| **Status** | ✅ Aprovado |
| **Data** | Junho 2026 |

---

## Contexto

Cada microsserviço precisa de um mecanismo de persistência. O padrão **Database per Service** (do DDD e microsserviços) determina que cada serviço deve ter sua própria base de dados para garantir autonomia e isolamento. É necessário escolher:

1. O banco de dados relacional
2. O ORM / mecanismo de acesso a dados

---

## Decisão

**SQL Server 2022** como banco de dados, com **Entity Framework Core 8** como ORM, em instâncias logicamente separadas (mesmo servidor Docker, bancos diferentes: `LancamentosDB` e `ConsolidadoDB`).

---

## Alternativas Consideradas

### PostgreSQL ❌ (tecnicamente equivalente)
- ✅ Open source, sem licença, excelente performance
- ✅ Suporte EF Core completo
- ❌ Escolhido SQL Server por maior familiaridade no ecossistema .NET corporativo
- **Nota:** A troca para PostgreSQL requer apenas mudança de pacote NuGet e connection string

### MongoDB (NoSQL) ❌
- ✅ Schema flexível, boa para dados sem estrutura fixa
- ❌ Dados financeiros têm estrutura bem definida e requerem ACID
- ❌ Transactions multi-documento mais complexas
- ❌ EF Core com MongoDB tem maturidade menor

### Dapper (micro-ORM) ao invés de EF Core ❌
- ✅ Performance superior, SQL explícito, controle total
- ❌ Mais código (sem migrations automáticas, sem change tracking)
- ❌ Para este porte, a produtividade do EF Core supera o ganho de performance
- **Nota:** Dapper pode ser usado para queries de leitura críticas no futuro (CQRS read side)

### Entity Framework Core 8 ✅ **ESCOLHIDA**
- ✅ Migrations automáticas (versionamento de schema)
- ✅ Change tracking (simplifica repositórios)
- ✅ LINQ, tipo-seguro, IntelliSense
- ✅ Suporte nativo a SQL Server, auditoria via interceptors
- ✅ Performance melhorada no EF 8 (compiled models, bulk operations)

---

## Trade-off Decisório

| Trade-off | Impacto | Por que foi aceito |
|-----------|---------|--------------------|
| **SQL Server tem custo de licença** em produção | Médio | Developer Edition gratuita para dev/test. Em produção, Azure SQL Basic começa em ~$5/mês. PostgreSQL é alternativa zero-custo com troca de 1 linha de código. |
| **EF Core mais lento que Dapper** em queries complexas | Baixo | Para este volume, a diferença é imperceptível (<5ms). Change tracking e migrations valem mais que o micro-ganho do SQL direto. |
| **Dois bancos** (mesmo servidor Docker) consomem mais memória | Baixo | SQL Server compartilha o buffer pool. O overhead real é de schema, não de instância separada. |
| **ORM abstrai o SQL** — dificulta otimização pontual | Baixo | Mitigado: EF Core permite `FromSqlRaw()` e `AsNoTracking()` para queries críticas. O plano inclui Dapper para o read side (CQRS) em evolução futura. |
| **Migrations versionadas** impedem rollback simples | Baixo | Down migrations incluídas em todas as migrations. Rollback é possível mas raro em sistemas financeiros (preferência: forward-only migration). |

**O trade-off que definiu a escolha:** dados financeiros exigem ACID. NoSQL foi descartado por isso. Entre SQL Server e PostgreSQL a diferença é irrelevante tecnicamente — SQL Server foi escolhido por maior familiaridade no ecossistema .NET corporativo, sem comprometer a arquitetura.

---

## Configuração de Auditoria (EF Interceptor)

```csharp
// SaveChangesInterceptor adiciona automaticamente:
// - CreatedAt, UpdatedAt (timestamps)
// - CreatedBy, UpdatedBy (usuário autenticado via IHttpContextAccessor)
// em todas as entidades que herdam de AuditableEntity
```

## Controle de Concorrência Otimista

A entidade `Lancamento` utiliza **RowVersion / ConcurrencyToken** via EF Core para detecção de concorrência otimista. O campo `RowVersion` é mapeado com `IsRowVersion()` na configuração do EF Core e incluído na cláusula `WHERE` de todo `UPDATE` e `DELETE`. Conflitos de concorrência resultam em `DbUpdateConcurrencyException`, que deve ser tratada na camada de aplicação com retry ou retorno de erro 409.

```csharp
// LancamentoConfiguration.cs
builder.Property(l => l.RowVersion)
    .IsRowVersion();
```

---

## Índices Importantes

**Lancamentos:**
```sql
CREATE INDEX IX_Lancamentos_Data ON Lancamentos(Data);
CREATE INDEX IX_Lancamentos_Status ON Lancamentos(Status);
CREATE INDEX IX_Lancamentos_Tipo_Data ON Lancamentos(Tipo, Data);
```

**ConsolidadoDiario:**
```sql
CREATE UNIQUE INDEX IX_ConsolidadoDiario_Data ON ConsolidadoDiario(Data);
```

---

## Consequências

**Positivas:**
- Cada serviço controla seu próprio schema independentemente
- Migrations versionadas no repositório
- Auditoria automática de todas as operações

**Negativas:**
- Duas instâncias de banco (mesmo que no mesmo server Docker)
- Não há joins entre os dois schemas (correto no padrão Database per Service)

---

## Histórico de Revisões

| Versão | Data | Autor | Descrição |
|--------|------|-------|-----------|
| 1.0 | Junho 2026 | Arquiteto de Soluções | Decisão inicial — MVP |
| 1.1 | Junho 2026 | Arquiteto de Soluções | Adicionada seção de trade-offs; incluso AuditLog como tabela persistida por EF Core |
| 1.2 | Junho 2026 | Arquiteto de Soluções | Adicionada seção de controle de concorrência otimista (RowVersion / ConcurrencyToken) |
- SQL Server requer licença em produção (mitigado com Developer Edition local ou Azure SQL)