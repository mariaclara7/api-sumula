using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public enum TipoSequencia
{
    Vitorias,
    Invencibilidade,
    SemVencer,
    Derrotas,
    Marcando,
    SemSofrerGol,
}

/// <param name="Atual">Quantos jogos seguidos até o último jogo disputado.</param>
/// <param name="Maior">A maior sequência na temporada.</param>
public sealed record Sequencia(TipoSequencia Tipo, int Atual, int Maior);

public sealed record PerfilGols(
    int Jogos,
    int GolsPro,
    int GolsContra,
    int SemSofrerGol,
    int SemMarcar,
    int MaisDeDoisGolsEMeio,
    int AmbosMarcam);

public sealed record EstatisticasTime(Time Time, PerfilGols Gols, IReadOnlyList<Sequencia> Sequencias);

public sealed record ResumoLiga(
    int Jogos,
    int Gols,
    int VitoriasMandante,
    int Empates,
    int VitoriasVisitante,
    int MaisDeDoisGolsEMeio,
    int AmbosMarcam);

public sealed record ResultadoEstatisticas(ResumoLiga Liga, IReadOnlyList<EstatisticasTime> Times);

/// <summary>Sequências (invencibilidade, jejum...) e o perfil de gols de cada time na temporada.</summary>
public static class CalculadoraEstatisticas
{
    private static readonly (TipoSequencia Tipo, Func<Partida, int, bool> Conta)[] Regras =
    [
        (TipoSequencia.Vitorias, (p, t) => p.ResultadoPara(t) == Resultado.Vitoria),
        (TipoSequencia.Invencibilidade, (p, t) => p.ResultadoPara(t) != Resultado.Derrota),
        (TipoSequencia.SemVencer, (p, t) => p.ResultadoPara(t) != Resultado.Vitoria),
        (TipoSequencia.Derrotas, (p, t) => p.ResultadoPara(t) == Resultado.Derrota),
        (TipoSequencia.Marcando, (p, t) => p.GolsDe(t) > 0),
        (TipoSequencia.SemSofrerGol, (p, t) => p.GolsContra(t) == 0),
    ];

    public static ResultadoEstatisticas Calcular(IReadOnlyCollection<Time> times, IReadOnlyCollection<Partida> partidas)
    {
        var jogadas = partidas.Where(p => p.TemResultado).OrderBy(p => p.Data).ThenBy(p => p.Id).ToList();

        var porTime = times
            .Select(time =>
            {
                var doTime = jogadas.Where(p => p.Envolve(time.Id)).ToList();
                return new EstatisticasTime(time, Perfil(doTime, time.Id), Sequencias(doTime, time.Id));
            })
            .ToList();

        var liga = new ResumoLiga(
            jogadas.Count,
            jogadas.Sum(p => p.GolsMandante!.Value + p.GolsVisitante!.Value),
            jogadas.Count(p => p.GolsMandante > p.GolsVisitante),
            jogadas.Count(p => p.GolsMandante == p.GolsVisitante),
            jogadas.Count(p => p.GolsMandante < p.GolsVisitante),
            jogadas.Count(p => p.GolsMandante + p.GolsVisitante > 2),
            jogadas.Count(p => p.GolsMandante > 0 && p.GolsVisitante > 0));

        return new ResultadoEstatisticas(liga, porTime);
    }

    private static PerfilGols Perfil(List<Partida> partidas, int timeId) => new(
        partidas.Count,
        partidas.Sum(p => p.GolsDe(timeId)),
        partidas.Sum(p => p.GolsContra(timeId)),
        partidas.Count(p => p.GolsContra(timeId) == 0),
        partidas.Count(p => p.GolsDe(timeId) == 0),
        partidas.Count(p => p.GolsDe(timeId) + p.GolsContra(timeId) > 2),
        partidas.Count(p => p.GolsDe(timeId) > 0 && p.GolsContra(timeId) > 0));

    private static IReadOnlyList<Sequencia> Sequencias(List<Partida> partidas, int timeId) =>
        Regras.Select(regra =>
        {
            int atual = 0, maior = 0;
            foreach (var partida in partidas)
            {
                atual = regra.Conta(partida, timeId) ? atual + 1 : 0;
                maior = Math.Max(maior, atual);
            }

            return new Sequencia(regra.Tipo, atual, maior);
        }).ToList();
}
