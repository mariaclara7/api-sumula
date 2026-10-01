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

    [Theory]
    [InlineData("CA Paranaense", "Paranaense", "CAP", "Athletico-PR", "CAP")]
    [InlineData("Clube do Remo", "Clube do Remo", "CRE", "Remo", "REM")]
    [InlineData("CA Mineiro", "Mineiro", "CAM", "Atlético-MG", "CAM")]
    [InlineData("Coritiba FBC", "Coritiba", "COR", "Coritiba", "CFC")]
    [InlineData("Grêmio FBPA", "Grêmio", "FBP", "Grêmio", "GRE")]
    [InlineData("SC Internacional", "Internacional", "SCI", "Internacional", "INT")]
    [InlineData("SC Corinthians Paulista", "Corinthians", "COR", "Corinthians", "COR")]
    [InlineData("São Paulo FC", "Sao Paulo", "SPF", "Sao Paulo", "SAO")]
    [InlineData("SE Palmeiras", "Palmeiras", "PAL", "Palmeiras", "PAL")]
    public void Ajusta_nome_curto_e_sigla_dos_clubes(
        string nome, string? nomeCurto, string sigla, string nomeCurtoEsperado, string siglaEsperada)
    {
        var nomes = Conversao.Nomes(new TimeFd(1, nome, nomeCurto, sigla, null));

        Assert.Equal(new Conversao.NomesTime(nome, nomeCurtoEsperado, siglaEsperada), nomes);
    }

    [Fact]
    public void Sem_nome_curto_nem_sigla_usa_o_nome()
    {
        Assert.Equal(new Conversao.NomesTime("Mirassol FC", "Mirassol FC", "MIR"),
            Conversao.Nomes(new TimeFd(1, "Mirassol FC", null, null, null)));
    }
}
