using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FluxoCaixaDiario.Lancamentos.Infrastructure.Persistence;

public sealed class LancamentosDbContextFactory : IDesignTimeDbContextFactory<LancamentosDbContext>
{
    public LancamentosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=LancamentosDB;User Id=sa;Password=FluxoCaixa@2024;TrustServerCertificate=True;",
                sql => sql.EnableRetryOnFailure(3))
            .Options;

        return new LancamentosDbContext(options);
    }
}