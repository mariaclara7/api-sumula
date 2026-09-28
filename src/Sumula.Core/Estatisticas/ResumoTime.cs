using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public sealed record ResumoTime(
    Time Time,
    LinhaClassificacao Geral,
    LinhaClassificacao Casa,
    LinhaClassificacao Fora,
    LinhaClassificacao PrimeiroTurno,
    LinhaClassificacao SegundoTurno,
    IReadOnlyList<PontoEvolucao> Evolucao,
    IReadOnlyList<Partida> UltimasPartidas,
    IReadOnlyList<Partida> ProximasPartidas)
{
    public const int QuantidadePartidasListadas = 5;

    /// <summary>Retorna null quando o time não disputa a competição.</summary>
    public static ResumoTime? Montar(int timeId, IReadOnlyCollection<Time> times, IReadOnlyCollection<Partida> partidas)
    {
        var time = times.FirstOrDefault(t => t.Id == timeId);
        if (time is null)
            return null;

        LinhaClassificacao LinhaDoTime(Filtro filtro) =>
            CalculadoraClassificacao.Calcular(times, partidas, filtro).Single(l => l.Time.Id == timeId);

        var doTime = partidas.Where(p => p.Envolve(timeId)).OrderBy(p => p.Data).ToList();

        return new ResumoTime(
            Time: time,
            Geral: LinhaDoTime(Filtro.Todos),
            Casa: LinhaDoTime(new Filtro(Mando: Mando.Casa)),
            Fora: LinhaDoTime(new Filtro(Mando: Mando.Fora)),
            PrimeiroTurno: LinhaDoTime(Filtro.DoRecorte(Recorte.PrimeiroTurno, times.Count)),
            SegundoTurno: LinhaDoTime(Filtro.DoRecorte(Recorte.SegundoTurno, times.Count)),
            Evolucao: CalculadoraEvolucao.Calcular(times, partidas)[timeId],
            UltimasPartidas: doTime.Where(p => p.TemResultado).TakeLast(QuantidadePartidasListadas).Reverse().ToList(),
            ProximasPartidas: doTime
                .Where(p => p.Status is StatusPartida.Agendada or StatusPartida.EmAndamento)
                .Take(QuantidadePartidasListadas)
                .ToList());
    }
}
