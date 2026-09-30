using Sumula.Exportador;

namespace Sumula.Core.Tests;

public class RotasEstaticasTests
{
    // Os mesmos exemplos estão em sumula/src/api/rotas.test.ts: as duas regras precisam bater.
    [Theory]
    [InlineData("/times", null, "/api/BSA/2026/times", "api/BSA/2026/times.json")]
    [InlineData("/times/1016", null, "/api/BSA/2026/times/1016", "api/BSA/2026/times/1016.json")]
    [InlineData("/classificacao", "tempo=jogoTodo;recorte=geral;mando=todos",
        "/api/BSA/2026/classificacao?mando=todos&recorte=geral&tempo=jogoTodo",
        "api/BSA/2026/classificacao__mando-todos__recorte-geral__tempo-jogoTodo.json")]
    [InlineData("/confronto", "timeB=1001;timeA=1000",
        "/api/BSA/2026/confronto?timeA=1000&timeB=1001",
        "api/BSA/2026/confronto__timeA-1000__timeB-1001.json")]
    public void Monta_url_e_nome_do_arquivo(string caminho, string? parametros, string url, string arquivo)
    {
        var dicionario = parametros?.Split(';').Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);

        var rota = RotasEstaticas.Criar("BSA", 2026, caminho, dicionario);

        Assert.Equal(url, rota.Url);
        Assert.Equal(arquivo, rota.Arquivo);
    }

    [Fact]
    public void Lista_tudo_o_que_o_site_pede()
    {
        var ids = Enumerable.Range(1, 20).ToList();

        var rotas = RotasEstaticas.Listar("BSA", 2026, ids).ToList();

        // 9 simples + 27 classificações + 20 times + 20 x 19 confrontos
        Assert.Equal(9 + 27 + 20 + 380, rotas.Count);
        Assert.Equal(rotas.Count, rotas.Select(r => r.Arquivo).Distinct().Count());
    }
}
