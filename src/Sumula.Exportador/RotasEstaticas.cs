namespace Sumula.Exportador;

/// <param name="Url">Endereço na API, ex.: /api/BSA/2026/classificacao?recorte=geral.</param>
/// <param name="Arquivo">Caminho do arquivo gerado, relativo à pasta de saída.</param>
public sealed record RotaEstatica(string Url, string Arquivo);

/// <summary>
/// Todos os endereços que o site usa, e o nome do arquivo estático de cada um.
/// </summary>
/// <remarks>
/// A regra do nome precisa ser igual à do site (sumula, src/api/rotas.ts):
/// <c>api/{competicao}/{temporada}{caminho}</c>, seguido de <c>__{chave}-{valor}</c> para cada parâmetro
/// em ordem alfabética (ordinal) da chave, e <c>.json</c> no fim.
/// Ex.: <c>/classificacao?tempo=jogoTodo&amp;recorte=geral&amp;mando=todos</c> vira
/// <c>api/BSA/2026/classificacao__mando-todos__recorte-geral__tempo-jogoTodo.json</c>.
/// </remarks>
public static class RotasEstaticas
{
    /// <summary>Endereços sem parâmetros usados pelo site.</summary>
    public static readonly string[] Simples =
        ["times", "partidas", "evolucao", "tempos", "artilharia", "probabilidades", "palpites", "matematica", "estatisticas"];

    public static readonly string[] Recortes = ["geral", "primeiroTurno", "segundoTurno"];
    public static readonly string[] Mandos = ["todos", "casa", "fora"];
    public static readonly string[] Tempos = ["jogoTodo", "primeiroTempo", "segundoTempo"];

    public static IEnumerable<RotaEstatica> Listar(string competicao, int temporada, IReadOnlyCollection<int> timeIds)
    {
        foreach (var caminho in Simples)
            yield return Criar(competicao, temporada, $"/{caminho}");

        foreach (var recorte in Recortes)
        foreach (var mando in Mandos)
        foreach (var tempo in Tempos)
        {
            yield return Criar(competicao, temporada, "/classificacao",
                new Dictionary<string, string> { ["recorte"] = recorte, ["mando"] = mando, ["tempo"] = tempo });
        }

        foreach (var id in timeIds)
            yield return Criar(competicao, temporada, $"/times/{id}");

        // O Comparador pede o confronto nos dois sentidos (A x B e B x A).
        foreach (var timeA in timeIds)
        foreach (var timeB in timeIds.Where(b => b != timeA))
        {
            yield return Criar(competicao, temporada, "/confronto",
                new Dictionary<string, string> { ["timeA"] = timeA.ToString(), ["timeB"] = timeB.ToString() });
        }
    }

    public static RotaEstatica Criar(
        string competicao, int temporada, string caminho, IReadOnlyDictionary<string, string>? parametros = null)
    {
        var baseUrl = $"/api/{competicao}/{temporada}{caminho}";
        var ordenados = (parametros ?? new Dictionary<string, string>())
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .ToList();

        var consulta = string.Join("&", ordenados.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var sufixo = string.Concat(ordenados.Select(p => $"__{p.Key}-{p.Value}"));

        return new RotaEstatica(
            consulta.Length > 0 ? $"{baseUrl}?{consulta}" : baseUrl,
            $"{baseUrl.TrimStart('/')}{sufixo}.json");
    }
}
