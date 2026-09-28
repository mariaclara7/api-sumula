using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public enum Recorte
{
    Geral,
    PrimeiroTurno,
    SegundoTurno,
}

public enum Mando
{
    Todos,
    Casa,
    Fora,
}

/// <summary>Define quais partidas entram em um cálculo.</summary>
public sealed record Filtro(int? RodadaInicial = null, int? RodadaFinal = null, Mando Mando = Mando.Todos)
{
    public static readonly Filtro Todos = new();

    /// <summary>
    /// Em pontos corridos cada turno tem (times - 1) rodadas: 19 no Brasileirão com 20 clubes.
    /// </summary>
    public static Filtro DoRecorte(Recorte recorte, int quantidadeTimes, Mando mando = Mando.Todos)
    {
        var rodadasPorTurno = Math.Max(quantidadeTimes - 1, 1);
        return recorte switch
        {
            Recorte.PrimeiroTurno => new Filtro(1, rodadasPorTurno, mando),
            Recorte.SegundoTurno => new Filtro(rodadasPorTurno + 1, null, mando),
            _ => new Filtro(null, null, mando),
        };
    }

    public bool IncluiRodada(Partida partida) =>
        (RodadaInicial is null || partida.Rodada >= RodadaInicial) &&
        (RodadaFinal is null || partida.Rodada <= RodadaFinal);

    public bool IncluiParaTime(Partida partida, int timeId) => Mando switch
    {
        Mando.Casa => partida.MandanteId == timeId,
        Mando.Fora => partida.VisitanteId == timeId,
        _ => partida.Envolve(timeId),
    };
}
