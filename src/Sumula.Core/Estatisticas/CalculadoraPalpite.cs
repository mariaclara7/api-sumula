using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public sealed record Placar(int Mandante, int Visitante);

/// <summary>Palpite para um jogo que ainda não aconteceu.</summary>
public sealed record Palpite(
    int PartidaId,
    double GolsEsperadosMandante,
    double GolsEsperadosVisitante,
    double VitoriaMandante,
    double Empate,
    double VitoriaVisitante,
    Placar PlacarMaisProvavel);

/// <summary>
/// Chances de vitória, empate e derrota de cada jogo restante, somando as probabilidades de todos os
/// placares possíveis. Os gols de cada lado seguem uma distribuição de Poisson com a média do <see cref="ModeloGols"/>.
/// </summary>
public static class CalculadoraPalpite
{
    /// <summary>Placares acima disso têm chance desprezível; o que sobra é redistribuído na normalização.</summary>
    private const int MaximoGols = 10;

    public static IReadOnlyList<Palpite> Calcular(IReadOnlyCollection<Time> times, IReadOnlyCollection<Partida> partidas)
    {
        var modelo = ModeloGols.Estimar(times, partidas);

        return partidas
            .Where(p => !p.TemResultado && p.Status != StatusPartida.Cancelada)
            .Select(p => Calcular(p.Id, modelo.GolsEsperados(p.MandanteId, p.VisitanteId)))
            .ToList();
    }

    public static Palpite Calcular(int partidaId, (double Mandante, double Visitante) golsEsperados)
    {
        var mandante = Distribuicao(golsEsperados.Mandante);
        var visitante = Distribuicao(golsEsperados.Visitante);

        double vitoriaMandante = 0, empate = 0, vitoriaVisitante = 0, total = 0, maior = -1;
        var placar = new Placar(0, 0);

        for (var m = 0; m <= MaximoGols; m++)
        {
            for (var v = 0; v <= MaximoGols; v++)
            {
                var chance = mandante[m] * visitante[v];
                total += chance;
                if (m > v) vitoriaMandante += chance;
                else if (m == v) empate += chance;
                else vitoriaVisitante += chance;

                if (chance > maior)
                {
                    maior = chance;
                    placar = new Placar(m, v);
                }
            }
        }

        return new Palpite(
            partidaId,
            Math.Round(golsEsperados.Mandante, 2),
            Math.Round(golsEsperados.Visitante, 2),
            vitoriaMandante / total,
            empate / total,
            vitoriaVisitante / total,
            placar);
    }

    /// <summary>P(gols = k) para k = 0..MaximoGols.</summary>
    private static double[] Distribuicao(double media)
    {
        var chances = new double[MaximoGols + 1];
        chances[0] = Math.Exp(-media);
        for (var k = 1; k <= MaximoGols; k++)
            chances[k] = chances[k - 1] * media / k;
        return chances;
    }
}
