using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sumula.Core.Modelos;

namespace Sumula.Coletor.Fotos;

/// <summary>Um jogador no Wikidata: data de nascimento (P569) e arquivo da foto no Commons (P18).</summary>
public sealed record EntidadeWikidata(string Id, DateOnly? Nascimento, string? Imagem);

/// <summary>Lê as respostas das APIs do Wikidata e do Commons (formatversion=2).</summary>
public static partial class LeituraWikimedia
{
    /// <summary>Ids (Q...) da busca <c>action=query&amp;list=search</c>.</summary>
    public static IReadOnlyList<string> Busca(JsonElement resposta) =>
        resposta.TryGetProperty("query", out var query) && query.TryGetProperty("search", out var busca)
            ? busca.EnumerateArray().Select(r => r.GetProperty("title").GetString()!).ToList()
            : [];

    /// <summary>Entidades de <c>action=wbgetentities</c>.</summary>
    public static IReadOnlyList<EntidadeWikidata> Entidades(JsonElement resposta)
    {
        if (!resposta.TryGetProperty("entities", out var entidades))
            return [];

        var lista = new List<EntidadeWikidata>();
        foreach (var entidade in entidades.EnumerateObject())
        {
            if (!entidade.Value.TryGetProperty("claims", out var claims))
                continue;

            var nascimento = Valor(claims, "P569") is { ValueKind: JsonValueKind.Object } tempo
                ? Data(tempo.GetProperty("time").GetString())
                : null;
            var imagem = Valor(claims, "P18") is { ValueKind: JsonValueKind.String } arquivo ? arquivo.GetString() : null;
            lista.Add(new EntidadeWikidata(entidade.Name, nascimento, imagem));
        }

        return lista;
    }

    /// <summary>Foto de <c>action=query&amp;prop=imageinfo&amp;iiprop=url|extmetadata</c> no Commons.</summary>
    public static FotoJogador? Foto(JsonElement resposta)
    {
        if (!resposta.TryGetProperty("query", out var query) || !query.TryGetProperty("pages", out var paginas))
            return null;

        foreach (var pagina in paginas.EnumerateArray())
        {
            if (!pagina.TryGetProperty("imageinfo", out var infos) || infos.GetArrayLength() == 0)
                continue;

            var info = infos[0];
            var url = Texto(info, "thumburl") ?? Texto(info, "url");
            var descricao = Texto(info, "descriptionurl");
            if (url is null || descricao is null)
                continue;

            info.TryGetProperty("extmetadata", out var meta);
            return new FotoJogador(url, descricao, SemHtml(Meta(meta, "Artist")), SemHtml(Meta(meta, "LicenseShortName")));
        }

        return null;
    }

    /// <summary>
    /// "+1997-06-20T00:00:00Z" → 1997-06-20. Datas só com ano ou mês ("+1997-00-00...") não servem para comparar
    /// e ficam nulas.
    /// </summary>
    public static DateOnly? Data(string? tempo) =>
        tempo is { Length: >= 11 } && DateOnly.TryParseExact(tempo.Substring(1, 10), "yyyy-MM-dd", out var data)
            ? data
            : null;

    /// <summary>O Commons devolve o autor em HTML (às vezes com link): fica só o texto.</summary>
    public static string? SemHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        var texto = Espacos().Replace(WebUtility.HtmlDecode(Tags().Replace(html, " ")), " ").Trim();
        if (texto.Length == 0)
            return null;
        return texto.Length <= 80 ? texto : texto[..79].TrimEnd() + "…";
    }

    /// <summary>Valor da afirmação preferida (ou da primeira normal) de uma propriedade.</summary>
    private static JsonElement? Valor(JsonElement claims, string propriedade)
    {
        if (!claims.TryGetProperty(propriedade, out var afirmacoes))
            return null;

        var validas = afirmacoes.EnumerateArray()
            .Where(a => a.GetProperty("rank").GetString() != "deprecated")
            .OrderBy(a => a.GetProperty("rank").GetString() == "preferred" ? 0 : 1);

        foreach (var afirmacao in validas)
        {
            if (afirmacao.GetProperty("mainsnak").TryGetProperty("datavalue", out var dado))
                return dado.GetProperty("value");
        }

        return null;
    }

    private static string? Texto(JsonElement elemento, string nome) =>
        elemento.TryGetProperty(nome, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;

    private static string? Meta(JsonElement meta, string nome) =>
        meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(nome, out var campo) ? Texto(campo, "value") : null;

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();
}
