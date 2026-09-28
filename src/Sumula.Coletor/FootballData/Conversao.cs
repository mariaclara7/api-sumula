using Sumula.Core.Modelos;

namespace Sumula.Coletor.FootballData;

public static class Conversao
{
    public static StatusPartida Status(string status) => status switch
    {
        "FINISHED" or "AWARDED" => StatusPartida.Encerrada,
        "IN_PLAY" or "PAUSED" or "LIVE" => StatusPartida.EmAndamento,
        "POSTPONED" or "SUSPENDED" => StatusPartida.Adiada,
        "CANCELLED" => StatusPartida.Cancelada,
        _ => StatusPartida.Agendada,
    };
}
