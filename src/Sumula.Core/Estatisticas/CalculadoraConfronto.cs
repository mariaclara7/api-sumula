using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public sealed record ResumoConfronto(
    int TimeAId,
    int TimeBId,
    int Jogos,
    int VitoriasA,
    int Empates,
    int VitoriasB,
    int GolsA,
    int GolsB,
    IReadOnlyList<Partida> Partidas);

public static class CalculadoraConfronto
{
    /// <summary>
    /// Resume os jogos entre dois times. <see cref="ResumoConfronto.Partidas"/> traz também
    /// os jogos ainda sem resultado, para mostrar o próximo encontro.
    /// </summary>
    public static ResumoConfronto Resumir(int timeAId, int timeBId, IEnumerable<Partida> partidas)
    {
        var entreOsDois = partidas
            .Where(p => p.Envolve(timeAId) && p.Envolve(timeBId) && timeAId != timeBId)
            .OrderBy(p => p.Data)
            .ToList();

        var comResultado = entreOsDois.Where(p => p.TemResultado).ToList();

        return new ResumoConfronto(
            TimeAId: timeAId,
            TimeBId: timeBId,
            Jogos: comResultado.Count,
            VitoriasA: comResultado.Count(p => p.ResultadoPara(timeAId) == Resultado.Vitoria),
            Empates: comResultado.Count(p => p.ResultadoPara(timeAId) == Resultado.Empate),
            VitoriasB: comResultado.Count(p => p.ResultadoPara(timeBId) == Resultado.Vitoria),
            GolsA: comResultado.Sum(p => p.GolsDe(timeAId)),
            GolsB: comResultado.Sum(p => p.GolsDe(timeBId)),
            Partidas: entreOsDois);
    }
}
