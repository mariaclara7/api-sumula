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
    int? GolsVisitante)
{
    /// <summary>Só partidas encerradas e com placar contam para as estatísticas.</summary>
    public bool TemResultado =>
        Status == StatusPartida.Encerrada && GolsMandante is not null && GolsVisitante is not null;

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
