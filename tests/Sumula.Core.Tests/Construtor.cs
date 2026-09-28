using Sumula.Core.Modelos;

namespace Sumula.Core.Tests;

/// <summary>Atalhos para montar cenários de teste.</summary>
internal static class Construtor
{
    private static readonly DateTimeOffset Inicio = new(2026, 3, 29, 16, 0, 0, TimeSpan.Zero);
    private static int _proximoId = 1;

    public static Time Time(int id, string nome) => new(id, nome, nome, nome[..3].ToUpperInvariant(), null);

    public static Partida Jogo(int rodada, int mandante, int golsMandante, int visitante, int golsVisitante) =>
        new(Interlocked.Increment(ref _proximoId), rodada, Inicio.AddDays(rodada * 7), StatusPartida.Encerrada,
            mandante, visitante, golsMandante, golsVisitante);

    public static Partida Agendado(int rodada, int mandante, int visitante) =>
        new(Interlocked.Increment(ref _proximoId), rodada, Inicio.AddDays(rodada * 7), StatusPartida.Agendada,
            mandante, visitante, null, null);
}
