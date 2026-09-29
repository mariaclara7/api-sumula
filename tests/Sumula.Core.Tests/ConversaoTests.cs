using Sumula.Coletor.FootballData;
using Sumula.Core.Modelos;

namespace Sumula.Core.Tests;

public class ConversaoTests
{
    private static PartidaFd Partida(params GolFd[] gols) => new(
        Id: 1,
        UtcDate: DateTimeOffset.UnixEpoch,
        Status: "FINISHED",
        Matchday: 1,
        HomeTeam: new TimeFd(10, "Casa", null, null, null),
        AwayTeam: new TimeFd(20, "Fora", null, null, null),
        Score: new PlacarFd(new GolsFd(2, 1), new GolsFd(1, 0)),
        Goals: [.. gols]);

    [Fact]
    public void Descobre_quem_marcou_pelo_placar_depois_do_gol()
    {
        // Fora de ordem de propósito, e o gol contra traz o autor do time que sofreu.
        var gols = Conversao.Gols(Partida(
            new GolFd(80, null, "OWN", new JogadorFd(5, "Zagueiro da Casa"), new GolsFd(2, 1)),
            new GolFd(12, null, "REGULAR", new JogadorFd(1, "Atacante"), new GolsFd(1, 0)),
            new GolFd(45, 2, "PENALTY", new JogadorFd(2, "Batedor"), new GolsFd(2, 0))));

        Assert.Equal([12, 45, 80], gols.Select(g => g.Minuto));
        Assert.Equal([10, 10, 20], gols.Select(g => g.TimeId));
        Assert.Equal([TipoGol.Normal, TipoGol.Penalti, TipoGol.Contra], gols.Select(g => g.Tipo));
        Assert.Equal(2, gols[1].Acrescimo);
    }

    [Fact]
    public void Ignora_gol_sem_minuto_ou_sem_placar()
    {
        var gols = Conversao.Gols(Partida(
            new GolFd(null, null, "REGULAR", null, new GolsFd(1, 0)),
            new GolFd(30, null, "REGULAR", null, null)));

        Assert.Empty(gols);
    }
}
