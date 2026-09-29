namespace Sumula.Core.Modelos;

public enum TipoGol
{
    Normal,
    Penalti,
    Contra,
}

/// <summary>
/// Um gol com o minuto. Só existe quando a fonte entrega os dados detalhados da partida
/// (no football-data.org, a partir do plano "Free + Deep Data").
/// </summary>
/// <param name="TimeId">Time que ganhou o gol no placar (em gol contra, o adversário de quem chutou).</param>
/// <param name="Acrescimo">Minutos de acréscimo: 45+2 vira Minuto 45 e Acrescimo 2.</param>
public sealed record Gol(int PartidaId, int Minuto, int? Acrescimo, int TimeId, TipoGol Tipo);
