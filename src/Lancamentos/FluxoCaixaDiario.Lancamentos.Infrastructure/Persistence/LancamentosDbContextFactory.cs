using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;

public sealed class LancamentosDbContextFactory : IDesignTimeDbContextFactory<LancamentosDbContext>
{
    public LancamentosDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__LancamentosDb")
            ?? throw new InvalidOperationException(
                "Variável de ambiente 'ConnectionStrings__LancamentosDb' não definida. " +
                "Execute o script 'scripts/init-user-secrets.ps1' ou defina a variável antes de rodar 'dotnet ef'.");

        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(3))
            .Options;

        return new LancamentosDbContext(options);
    }
}
