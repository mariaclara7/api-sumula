using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sumula.Api;
using Sumula.Coletor;
using Sumula.Coletor.FootballData;
using Sumula.Coletor.Fotos;
using Sumula.Core.Fotos;
using Sumula.Core.Modelos;

namespace Sumula.Core.Tests;

public class FotosTests
{
    // Respostas no formato das APIs (formatversion=2), reduzidas ao que o código lê.
    private const string BuscaHulk = """{"query":{"search":[{"ns":0,"title":"Q320452"},{"ns":0,"title":"Q999"}]}}""";

    private const string EntidadesHulk = """
        {"entities":{
          "Q320452":{"id":"Q320452","claims":{
            "P569":[{"mainsnak":{"datavalue":{"value":{"time":"+1986-07-25T00:00:00Z","precision":11},"type":"time"}},"rank":"normal"}],
            "P18":[
              {"mainsnak":{"datavalue":{"value":"Hulk velho.jpg","type":"string"}},"rank":"deprecated"},
              {"mainsnak":{"datavalue":{"value":"Hulk 2023.jpg","type":"string"}},"rank":"normal"},
              {"mainsnak":{"datavalue":{"value":"Hulk 2024.jpg","type":"string"}},"rank":"preferred"}]}},
          "Q999":{"id":"Q999","claims":{
            "P569":[{"mainsnak":{"datavalue":{"value":{"time":"+1990-01-01T00:00:00Z","precision":11},"type":"time"}},"rank":"normal"}]}}
        }}
        """;

    private const string FotoHulk = """
        {"query":{"pages":[{"title":"File:Hulk 2024.jpg","imageinfo":[{
          "url":"https://upload.wikimedia.org/wikipedia/commons/a/ab/Hulk_2024.jpg",
          "thumburl":"https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Hulk_2024.jpg/480px-Hulk_2024.jpg",
          "descriptionurl":"https://commons.wikimedia.org/wiki/File:Hulk_2024.jpg",
          "extmetadata":{
            "Artist":{"value":"<a href=\"//commons.wikimedia.org/wiki/User:Fot%C3%B3grafo\">Jo&atilde;o   Fot&oacute;grafo</a>"},
            "LicenseShortName":{"value":"CC BY-SA 4.0"}}}]}]}}
        """;

    private static JsonElement Json(string texto) => JsonDocument.Parse(texto).RootElement;

    [Fact]
    public void Le_a_busca_e_as_entidades_do_wikidata()
    {
        Assert.Equal(["Q320452", "Q999"], LeituraWikimedia.Busca(Json(BuscaHulk)));

        var entidades = LeituraWikimedia.Entidades(Json(EntidadesHulk));

        // A imagem preferida vence a normal, e a descontinuada é ignorada.
        Assert.Contains(new EntidadeWikidata("Q320452", new DateOnly(1986, 7, 25), "Hulk 2024.jpg"), entidades);
        Assert.Contains(new EntidadeWikidata("Q999", new DateOnly(1990, 1, 1), null), entidades);
    }

    [Fact]
    public void Le_a_foto_do_commons_com_o_credito_sem_html()
    {
        var foto = LeituraWikimedia.Foto(Json(FotoHulk));

        Assert.Equal(new FotoJogador(
            "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Hulk_2024.jpg/480px-Hulk_2024.jpg",
            "https://commons.wikimedia.org/wiki/File:Hulk_2024.jpg",
            "João Fotógrafo",
            "CC BY-SA 4.0"), foto);
    }

    [Theory]
    [InlineData("+1986-07-25T00:00:00Z", "1986-07-25")]
    [InlineData("+1986-00-00T00:00:00Z", null)] // só o ano: não serve para confirmar
    [InlineData(null, null)]
    public void Data_de_nascimento_so_com_dia_completo(string? tempo, string? esperado) =>
        Assert.Equal(esperado is null ? null : DateOnly.Parse(esperado), LeituraWikimedia.Data(tempo));

    [Fact]
    public void Escolhe_so_quem_nasceu_no_mesmo_dia()
    {
        var entidades = new[]
        {
            new EntidadeWikidata("Q1", new DateOnly(1997, 6, 20), "a.jpg"),
            new EntidadeWikidata("Q2", new DateOnly(1999, 1, 1), "b.jpg"),
            new EntidadeWikidata("Q1", new DateOnly(1997, 6, 20), "a.jpg"),
        };

        Assert.Equal(["Q1"], BuscadorFotos.Escolher(entidades, new DateOnly(1997, 6, 20)).Select(e => e.Id));
        Assert.Empty(BuscadorFotos.Escolher(entidades, new DateOnly(2000, 1, 1)));
    }

    [Fact]
    public void Manual_e_foto_achada_nao_sao_buscados_de_novo()
    {
        var hoje = new DateOnly(2026, 10, 3);
        var foto = new FotoJogador("u", "p", null, null);
        var arquivo = new ArquivoFotos
        {
            Jogadores =
            {
                [1] = new RegistroFoto("Com foto", "Q1", foto, hoje.AddDays(-400)),
                [2] = new RegistroFoto("Manual sem foto", null, null, hoje.AddDays(-400), Manual: true),
                [3] = new RegistroFoto("Sem foto, recente", null, null, hoje.AddDays(-5)),
                [4] = new RegistroFoto("Sem foto, antigo", null, null, hoje.AddDays(-30)),
            },
        };

        Assert.False(BuscadorFotos.PrecisaBuscar(arquivo, 1, hoje));
        Assert.False(BuscadorFotos.PrecisaBuscar(arquivo, 2, hoje));
        Assert.False(BuscadorFotos.PrecisaBuscar(arquivo, 3, hoje));
        Assert.True(BuscadorFotos.PrecisaBuscar(arquivo, 4, hoje));
        Assert.True(BuscadorFotos.PrecisaBuscar(arquivo, 5, hoje));
    }

    [Fact]
    public void Arquivo_fica_legivel_e_volta_igual()
    {
        var arquivo = new ArquivoFotos
        {
            Jogadores =
            {
                [20] = new RegistroFoto("Sem Foto", null, null, new DateOnly(2026, 10, 3)),
                [10] = new RegistroFoto("Hulk", "Q320452",
                    new FotoJogador("https://x/480px-a.jpg", "https://c/File:a.jpg", "João Fotógrafo", "CC BY-SA 4.0"),
                    new DateOnly(2026, 10, 3)),
            },
        };

        var json = arquivo.ParaJson();

        Assert.Contains("João Fotógrafo", json); // sem "ã"
        Assert.DoesNotContain("manual", json); // valores padrão não poluem o arquivo
        Assert.True(json.IndexOf("\"10\"", StringComparison.Ordinal) < json.IndexOf("\"20\"", StringComparison.Ordinal));
        Assert.Equal(json, ArquivoFotos.Ler(json).ParaJson());
    }

    [Fact]
    public void Api_le_o_arquivo_embutido()
    {
        var artilheiro = new Artilheiro(1, "Alguém", 10, 5, 3, 1, 0);

        // O arquivo do repositório pode ou não ter esse jogador; o importante é carregar sem erro.
        Assert.Equal(artilheiro.JogadorId, new CatalogoFotos().ComFoto(artilheiro).JogadorId);
    }

    [Fact]
    public async Task Busca_completa_grava_a_foto_confirmada_pela_data_de_nascimento()
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"fotos-{Guid.NewGuid():N}.json");
        var respostas = new RespostasFalsas
        {
            ["football-data/competitions/BSA/scorers"] = """
                {"scorers":[
                  {"player":{"id":10,"name":"Hulk","firstName":"Givanildo","lastName":"Vieira de Sousa","dateOfBirth":"1986-07-25"},
                   "team":{"id":1766,"name":"CA Mineiro"},"goals":12},
                  {"player":{"id":20,"name":"Sem Data"},"team":{"id":1766,"name":"CA Mineiro"},"goals":3}]}
                """,
            ["wikidata.org/w/api.php?action=query"] = BuscaHulk,
            ["wikidata.org/w/api.php?action=wbgetentities"] = EntidadesHulk,
            ["commons.wikimedia.org"] = FotoHulk,
        };

        try
        {
            var buscador = new BuscadorFotos(
                new ClienteFootballData(new HttpClient(respostas) { BaseAddress = new Uri("https://football-data/") },
                    NullLogger<ClienteFootballData>.Instance),
                new ClienteWikimedia(new HttpClient(respostas)),
                Options.Create(new OpcoesColeta { Competicoes = ["BSA"], Temporada = 2026 }),
                TimeProvider.System,
                NullLogger<BuscadorFotos>.Instance);

            await buscador.ExecutarAsync(caminho, CancellationToken.None);

            var arquivo = ArquivoFotos.Ler(await File.ReadAllTextAsync(caminho));
            Assert.Equal("Q320452", arquivo.Jogadores[10].Wikidata);
            Assert.Equal("João Fotógrafo", arquivo.Jogadores[10].Foto?.Autor);
            Assert.Null(arquivo.Jogadores[20].Foto);
            Assert.Contains(respostas.Pedidos, p => p.Contains("Hulk%20haswbstatement"));
            Assert.Contains(respostas.Pedidos, p => p.Contains("Givanildo%20Vieira%20de%20Sousa"));
        }
        finally
        {
            File.Delete(caminho);
        }
    }

    /// <summary>Responde pelo primeiro trecho de endereço que bater.</summary>
    private sealed class RespostasFalsas : HttpMessageHandler, IEnumerable<KeyValuePair<string, string>>
    {
        private readonly List<KeyValuePair<string, string>> respostas = [];
        public List<string> Pedidos { get; } = [];

        public string this[string trecho] { set => respostas.Add(new(trecho, value)); }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken ct)
        {
            var url = pedido.RequestUri!.AbsoluteUri;
            Pedidos.Add(url);
            var corpo = respostas.FirstOrDefault(r => url.Contains(r.Key)).Value;
            return Task.FromResult(corpo is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(corpo, Encoding.UTF8, "application/json") });
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => respostas.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
