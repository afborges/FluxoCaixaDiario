using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;

public sealed class ConsolidadoDbContextFactory : IDesignTimeDbContextFactory<ConsolidadoDbContext>
{
    public ConsolidadoDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__ConsolidadoDb")
            ?? throw new InvalidOperationException(
                "Variável de ambiente 'ConnectionStrings__ConsolidadoDb' não definida. " +
                "Execute o script 'scripts/init-user-secrets.ps1' ou defina a variável antes de rodar 'dotnet ef'.");

        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(3))
            .Options;

        return new ConsolidadoDbContext(options);
    }
}
