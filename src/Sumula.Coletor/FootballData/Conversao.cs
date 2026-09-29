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

    public static TipoGol TipoGol(string? tipo) => tipo switch
    {
        "PENALTY" => Core.Modelos.TipoGol.Penalti,
        "OWN" => Core.Modelos.TipoGol.Contra,
        _ => Core.Modelos.TipoGol.Normal,
    };

    public sealed record GolConvertido(int Minuto, int? Acrescimo, int TimeId, TipoGol Tipo, string? Autor);

    /// <summary>
    /// Converte os gols da partida. O time que marcou é deduzido do placar logo após cada gol
    /// (o lado que aumentou), o que funciona também para gol contra, sem depender de como a
    /// fonte preenche o time do autor. Gols sem minuto ou sem placar são ignorados.
    /// </summary>
    public static IReadOnlyList<GolConvertido> Gols(PartidaFd partida)
    {
        var convertidos = new List<GolConvertido>();
        int mandante = 0, visitante = 0;

        var ordenados = (partida.Goals ?? [])
            .Where(g => g.Minute is not null && g.Score?.Home is not null && g.Score.Away is not null)
            .OrderBy(g => g.Score!.Home + g.Score.Away);

        foreach (var gol in ordenados)
        {
            var (casa, fora) = (gol.Score!.Home!.Value, gol.Score.Away!.Value);
            int? timeId = casa > mandante ? partida.HomeTeam.Id : fora > visitante ? partida.AwayTeam.Id : null;
            (mandante, visitante) = (casa, fora);

            if (timeId is null)
                continue;

            convertidos.Add(new GolConvertido(
                gol.Minute!.Value,
                gol.InjuryTime is > 0 ? gol.InjuryTime : null,
                timeId.Value,
                TipoGol(gol.Type),
                gol.Scorer?.Name));
        }

        return convertidos;
    }
}
