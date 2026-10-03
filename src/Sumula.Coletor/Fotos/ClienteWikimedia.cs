using System.Text.Json;
using Sumula.Core.Modelos;

namespace Sumula.Coletor.Fotos;

/// <summary>
/// Busca jogadores no Wikidata e as fotos no Wikimedia Commons. As APIs são abertas; a regra de uso pede
/// um User-Agent que identifique o site (configurado no Program.cs) e requisições uma de cada vez.
/// </summary>
public class ClienteWikimedia(HttpClient http)
{
    private const string Wikidata = "https://www.wikidata.org/w/api.php";
    private const string Commons = "https://commons.wikimedia.org/w/api.php";

    /// <summary>Q937857 = jogador de futebol (P106 = ocupação).</summary>
    public async Task<IReadOnlyList<string>> BuscarJogadoresAsync(string nome, CancellationToken ct) =>
        LeituraWikimedia.Busca(await ObterAsync(
            $"{Wikidata}?action=query&list=search&srnamespace=0&srlimit=10&format=json&formatversion=2" +
            $"&srsearch={Uri.EscapeDataString($"{nome} haswbstatement:P106=Q937857")}", ct));

    public async Task<IReadOnlyList<EntidadeWikidata>> ObterEntidadesAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var lista = string.Join('|', ids);
        if (lista.Length == 0)
            return [];

        return LeituraWikimedia.Entidades(await ObterAsync(
            $"{Wikidata}?action=wbgetentities&props=claims&format=json&formatversion=2&ids={Uri.EscapeDataString(lista)}", ct));
    }

    public async Task<FotoJogador?> ObterFotoAsync(string arquivo, CancellationToken ct) =>
        LeituraWikimedia.Foto(await ObterAsync(
            $"{Commons}?action=query&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=480&format=json&formatversion=2" +
            $"&titles={Uri.EscapeDataString("File:" + arquivo)}", ct));

    private async Task<JsonElement> ObterAsync(string url, CancellationToken ct)
    {
        using var resposta = await http.GetAsync(url, ct);
        resposta.EnsureSuccessStatusCode();
        await using var corpo = await resposta.Content.ReadAsStreamAsync(ct);
        using var documento = await JsonDocument.ParseAsync(corpo, cancellationToken: ct);
        return documento.RootElement.Clone();
    }
}
