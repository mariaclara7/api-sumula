using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

/// <summary>Quantos jogos o time estava em uma situação no intervalo e terminou em outra.</summary>
public sealed record TransicaoIntervalo(Resultado NoIntervalo, Resultado NoFinal, int Jogos);

/// <summary>Gols por faixa de 15 minutos; índice 0 = 1–15, 2 = 31–45 com acréscimos, 5 = 76–90 com acréscimos.</summary>
public sealed record FaixasMinuto(IReadOnlyList<int> Marcados, IReadOnlyList<int> Sofridos);

public sealed record DesempenhoPorTempo(
    Time Time,
    int Jogos,
    int GolsProPrimeiroTempo,
    int GolsContraPrimeiroTempo,
    int GolsProSegundoTempo,
    int GolsContraSegundoTempo,
    int PontosNoIntervalo,
    int Pontos,
    int ViradasAFavor,
    int ViradasContra,
    int PontosPerdidosVencendo,
    int PontosConquistadosPerdendo,
    IReadOnlyList<TransicaoIntervalo> Transicoes,
    FaixasMinuto? Faixas)
{
    /// <summary>Pontos ganhos (ou perdidos, se negativo) em relação ao placar do intervalo.</summary>
    public int PontosDepoisDoIntervalo => Pontos - PontosNoIntervalo;
}

public sealed record ResultadoTempos(
    bool TemFaixas,
    IReadOnlyList<string> RotulosFaixas,
    IReadOnlyList<DesempenhoPorTempo> Times);

/// <summary>
/// Compara o 1º e o 2º tempo a partir do placar do intervalo, que o plano gratuito do football-data.org
/// já entrega. Quando há gols com minuto (plano com dados detalhados), soma também as faixas de 15 minutos.
/// </summary>
public static class CalculadoraTempos
{
    public static readonly IReadOnlyList<string> RotulosFaixas = ["1–15", "16–30", "31–45+", "46–60", "61–75", "76–90+"];

    private static readonly Resultado[] Resultados = [Resultado.Vitoria, Resultado.Empate, Resultado.Derrota];

    public static ResultadoTempos Calcular(
        IReadOnlyCollection<Time> times,
        IReadOnlyCollection<Partida> partidas,
        IReadOnlyCollection<Gol>? gols = null)
    {
        var comIntervalo = partidas.Where(p => p.TemIntervalo).ToList();
        var faixas = CalcularFaixas(partidas, gols ?? []);

        var desempenhos = times
            .Select(time => Calcular(time, comIntervalo.Where(p => p.Envolve(time.Id)).ToList(), faixas?.GetValueOrDefault(time.Id)))
            .OrderByDescending(d => d.PontosDepoisDoIntervalo)
            .ThenByDescending(d => d.Pontos)
            .ThenBy(d => d.Time.Nome, StringComparer.CurrentCulture)
            .ToList();

        return new ResultadoTempos(faixas is not null, RotulosFaixas, desempenhos);
    }

    public static int FaixaDoMinuto(int minuto, int? acrescimo)
    {
        if (minuto <= 45)
            return acrescimo is > 0 && minuto == 45 ? 2 : Math.Clamp((minuto - 1) / 15, 0, 2);

        return Math.Clamp(3 + (minuto - 46) / 15, 3, 5);
    }

    private static DesempenhoPorTempo Calcular(Time time, List<Partida> partidas, FaixasMinuto? faixas)
    {
        var transicoes = new Dictionary<(Resultado, Resultado), int>();
        int golsPro1 = 0, golsContra1 = 0, golsPro2 = 0, golsContra2 = 0;
        int pontosIntervalo = 0, pontos = 0, viradasAFavor = 0, viradasContra = 0, perdidosVencendo = 0, conquistadosPerdendo = 0;

        foreach (var partida in partidas)
        {
            var primeiro = partida.NoTempo(Tempo.PrimeiroTempo)!;
            var segundo = partida.NoTempo(Tempo.SegundoTempo)!;
            golsPro1 += primeiro.GolsDe(time.Id);
            golsContra1 += primeiro.GolsContra(time.Id);
            golsPro2 += segundo.GolsDe(time.Id);
            golsContra2 += segundo.GolsContra(time.Id);

            var noIntervalo = primeiro.ResultadoPara(time.Id)!.Value;
            var noFinal = partida.ResultadoPara(time.Id)!.Value;
            transicoes[(noIntervalo, noFinal)] = transicoes.GetValueOrDefault((noIntervalo, noFinal)) + 1;

            pontosIntervalo += Pontos(noIntervalo);
            pontos += Pontos(noFinal);

            if (noIntervalo == Resultado.Derrota && noFinal == Resultado.Vitoria) viradasAFavor++;
            if (noIntervalo == Resultado.Vitoria && noFinal == Resultado.Derrota) viradasContra++;
            if (noIntervalo == Resultado.Vitoria) perdidosVencendo += 3 - Pontos(noFinal);
            if (noIntervalo == Resultado.Derrota) conquistadosPerdendo += Pontos(noFinal);
        }

        return new DesempenhoPorTempo(
            time,
            partidas.Count,
            golsPro1,
            golsContra1,
            golsPro2,
            golsContra2,
            pontosIntervalo,
            pontos,
            viradasAFavor,
            viradasContra,
            perdidosVencendo,
            conquistadosPerdendo,
            Resultados
                .SelectMany(intervalo => Resultados.Select(final =>
                    new TransicaoIntervalo(intervalo, final, transicoes.GetValueOrDefault((intervalo, final)))))
                .ToList(),
            faixas);
    }

    private static Dictionary<int, FaixasMinuto>? CalcularFaixas(IEnumerable<Partida> partidas, IReadOnlyCollection<Gol> gols)
    {
        if (gols.Count == 0)
            return null;

        var partidasPorId = partidas.Where(p => p.TemResultado).ToDictionary(p => p.Id);
        var marcados = new Dictionary<int, int[]>();
        var sofridos = new Dictionary<int, int[]>();

        foreach (var gol in gols)
        {
            if (!partidasPorId.TryGetValue(gol.PartidaId, out var partida) || !partida.Envolve(gol.TimeId))
                continue;

            var adversario = partida.MandanteId == gol.TimeId ? partida.VisitanteId : partida.MandanteId;
            var faixa = FaixaDoMinuto(gol.Minuto, gol.Acrescimo);
            Contar(marcados, gol.TimeId)[faixa]++;
            Contar(sofridos, adversario)[faixa]++;
        }

        return marcados.Keys.Union(sofridos.Keys).ToDictionary(
            timeId => timeId,
            timeId => new FaixasMinuto(
                marcados.GetValueOrDefault(timeId) ?? new int[RotulosFaixas.Count],
                sofridos.GetValueOrDefault(timeId) ?? new int[RotulosFaixas.Count]));
    }

    private static int[] Contar(Dictionary<int, int[]> contagem, int timeId)
    {
        if (!contagem.TryGetValue(timeId, out var faixas))
            contagem[timeId] = faixas = new int[RotulosFaixas.Count];
        return faixas;
    }

    private static int Pontos(Resultado resultado) => resultado switch
    {
        Resultado.Vitoria => 3,
        Resultado.Empate => 1,
        _ => 0,
    };
}
