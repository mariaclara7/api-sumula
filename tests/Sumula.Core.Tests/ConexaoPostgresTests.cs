using Sumula.Data;

namespace Sumula.Core.Tests;

public class ConexaoPostgresTests
{
    [Fact]
    public void Converte_url_do_neon_para_o_formato_do_npgsql()
    {
        var conexao = ConexaoPostgres.Normalizar(
            "postgresql://maria:s%40nha@ep-exemplo.sa-east-1.aws.neon.tech/sumula?sslmode=require");

        Assert.Contains("Host=ep-exemplo.sa-east-1.aws.neon.tech", conexao);
        Assert.Contains("Port=5432", conexao);
        Assert.Contains("Database=sumula", conexao);
        Assert.Contains("Username=maria", conexao);
        Assert.Contains("Password=s@nha", conexao);
        Assert.Contains("SSL Mode=Require", conexao);
    }

    [Fact]
    public void Mantem_conexao_que_ja_esta_no_formato_do_npgsql()
    {
        const string conexao = "Host=localhost;Database=sumula";
        Assert.Equal(conexao, ConexaoPostgres.Normalizar(conexao));
    }
}
