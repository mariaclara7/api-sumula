using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Sumula.Data;

public static class ConexaoPostgres
{
    /// <summary>
    /// Lê a conexão de <c>ConnectionStrings:Sumula</c> ou, se não houver, da variável <c>DATABASE_URL</c>.
    /// </summary>
    public static string Obter(IConfiguration configuracao)
    {
        var valor = configuracao.GetConnectionString("Sumula") ?? configuracao["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidOperationException(
                "Conexão com o banco não configurada. Defina ConnectionStrings__Sumula ou DATABASE_URL.");

        return Normalizar(valor);
    }

    /// <summary>
    /// Neon, Supabase e Render entregam a conexão no formato URL (postgres://usuario:senha@host/banco),
    /// que o Npgsql não aceita diretamente. Converte para "Host=...;Username=...".
    /// </summary>
    public static string Normalizar(string conexao)
    {
        if (!conexao.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !conexao.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return conexao;

        var uri = new Uri(conexao);
        var credenciais = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credenciais[0]),
            Password = credenciais.Length > 1 ? Uri.UnescapeDataString(credenciais[1]) : null,
        };

        var parametros = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var parametro in parametros)
        {
            var partes = parametro.Split('=', 2);
            if (partes[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase) && partes.Length == 2 &&
                Enum.TryParse<SslMode>(partes[1], ignoreCase: true, out var sslMode))
                builder.SslMode = sslMode;
        }

        return builder.ConnectionString;
    }

    public static DbContextOptionsBuilder UsarPostgres(this DbContextOptionsBuilder options, string conexao) =>
        options.UseNpgsql(conexao).UseSnakeCaseNamingConvention();
}
