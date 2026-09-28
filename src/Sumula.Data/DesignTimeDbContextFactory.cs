using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sumula.Data;

/// <summary>Usada apenas pelo "dotnet ef" para gerar migrations.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SumulaDbContext>
{
    public SumulaDbContext CreateDbContext(string[] args)
    {
        var conexao = Environment.GetEnvironmentVariable("ConnectionStrings__Sumula")
            ?? "Host=localhost;Port=5432;Database=sumula;Username=sumula;Password=sumula";

        var options = new DbContextOptionsBuilder<SumulaDbContext>();
        options.UsarPostgres(conexao);
        return new SumulaDbContext(options.Options);
    }
}
