namespace Sumula.Core.Modelos;

public enum StatusPartida
{
    Agendada,
    EmAndamento,
    Encerrada,
    Adiada,
    Cancelada,
}

public sealed record Partida(
    int Id,
    int Rodada,
    DateTimeOffset Data,
    StatusPartida Status,
    int MandanteId,
    int VisitanteId,
    int? GolsMandante,
    int? GolsVisitante,
    int? GolsMandanteIntervalo = null,
    int? GolsVisitanteIntervalo = null)
{
    /// <summary>Só partidas encerradas e com placar contam para as estatísticas.</summary>
    public bool TemResultado =>
        Status == StatusPartida.Encerrada && GolsMandante is not null && GolsVisitante is not null;

    /// <summary>Partida encerrada que também tem o placar do intervalo.</summary>
    public bool TemIntervalo => TemResultado && GolsMandanteIntervalo is not null && GolsVisitanteIntervalo is not null;

    /// <summary>
    /// A partida como se valesse só um dos tempos: no 1º, o placar do intervalo; no 2º, só os gols
    /// depois do intervalo. Retorna null quando falta o placar do intervalo.
    /// </summary>
    public Partida? NoTempo(Tempo tempo) => tempo switch
    {
        Tempo.JogoTodo => this,
        _ when !TemIntervalo => null,
        Tempo.PrimeiroTempo => this with { GolsMandante = GolsMandanteIntervalo, GolsVisitante = GolsVisitanteIntervalo },
        _ => this with
        {
            GolsMandante = GolsMandante - GolsMandanteIntervalo,
            GolsVisitante = GolsVisitante - GolsVisitanteIntervalo,
        },
    };

    /// <summary>Resultado do time no intervalo, ou null se não houver o placar.</summary>
    public Resultado? ResultadoNoIntervaloPara(int timeId) => NoTempo(Tempo.PrimeiroTempo)?.ResultadoPara(timeId);

    public bool Envolve(int timeId) => MandanteId == timeId || VisitanteId == timeId;

    public int GolsDe(int timeId) => (timeId == MandanteId ? GolsMandante : GolsVisitante) ?? 0;

    public int GolsContra(int timeId) => (timeId == MandanteId ? GolsVisitante : GolsMandante) ?? 0;

    public Resultado? ResultadoPara(int timeId)
    {
        if (!TemResultado || !Envolve(timeId))
            return null;

        var diferenca = GolsDe(timeId) - GolsContra(timeId);
        return diferenca > 0 ? Resultado.Vitoria : diferenca < 0 ? Resultado.Derrota : Resultado.Empate;
    }
}

public enum Resultado
{
    Vitoria,
    Empate,
    Derrota,
}

public enum Tempo
{
    JogoTodo,
    PrimeiroTempo,
    SegundoTempo,
}
