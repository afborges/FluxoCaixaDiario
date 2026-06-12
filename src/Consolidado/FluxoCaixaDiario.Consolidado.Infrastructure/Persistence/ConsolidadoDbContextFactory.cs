using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FluxoCaixaDiario.Consolidado.Infrastructure.Persistence;

public sealed class ConsolidadoDbContextFactory : IDesignTimeDbContextFactory<ConsolidadoDbContext>
{
    public ConsolidadoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=ConsolidadoDB;User Id=sa;Password=FluxoCaixa@2024;TrustServerCertificate=True;",
                sql => sql.EnableRetryOnFailure(3))
            .Options;

        return new ConsolidadoDbContext(options);
    }
}