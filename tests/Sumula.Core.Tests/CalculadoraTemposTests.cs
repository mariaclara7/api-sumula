using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class CalculadoraTemposTests
{
    private static readonly Time Bahia = Time(1, "Bahia");
    private static readonly Time Vitoria = Time(2, "Vitória");
    private static readonly Time Sport = Time(3, "Sport");
    private static readonly Time[] Times = [Bahia, Vitoria, Sport];

    [Fact]
    public void Separa_os_gols_de_cada_tempo()
    {
        Partida[] partidas =
        [
            JogoComIntervalo(1, Bahia.Id, Vitoria.Id, 3, 1, 1, 1), // Bahia: 1:1 no 1º tempo, 2:0 no 2º
            JogoComIntervalo(2, Sport.Id, Bahia.Id, 2, 1, 0, 1), // Bahia: 1:0 no 1º tempo, 0:2 no 2º
        ];

        var bahia = CalculadoraTempos.Calcular(Times, partidas).Times.Single(t => t.Time == Bahia);

        Assert.Equal(2, bahia.Jogos);
        Assert.Equal((2, 1), (bahia.GolsProPrimeiroTempo, bahia.GolsContraPrimeiroTempo));
        Assert.Equal((2, 2), (bahia.GolsProSegundoTempo, bahia.GolsContraSegundoTempo));
    }

    [Fact]
    public void Conta_viradas_e_pontos_ganhos_ou_perdidos_depois_do_intervalo()
    {
        Partida[] partidas =
        [
            JogoComIntervalo(1, Bahia.Id, Vitoria.Id, 2, 1, 0, 1), // Bahia perdia e virou: +3
            JogoComIntervalo(2, Sport.Id, Bahia.Id, 2, 1, 0, 1), // Bahia vencia e tomou a virada: -3
            JogoComIntervalo(3, Bahia.Id, Sport.Id, 1, 1, 1, 0), // Bahia vencia e cedeu o empate: -2
        ];

        var resultado = CalculadoraTempos.Calcular(Times, partidas);
        var bahia = resultado.Times.Single(t => t.Time == Bahia);

        Assert.Equal((1, 1), (bahia.ViradasAFavor, bahia.ViradasContra));
        Assert.Equal(6, bahia.PontosNoIntervalo); // D, V, V no intervalo
        Assert.Equal(4, bahia.Pontos); // V, D, E no final
        Assert.Equal(-2, bahia.PontosDepoisDoIntervalo);
        Assert.Equal(5, bahia.PontosPerdidosVencendo);
        Assert.Equal(3, bahia.PontosConquistadosPerdendo);

        Assert.Equal(9, bahia.Transicoes.Count);
        Assert.Equal(1, bahia.Transicoes.Single(t => t is { NoIntervalo: Resultado.Vitoria, NoFinal: Resultado.Empate }).Jogos);
        Assert.Equal(3, bahia.Transicoes.Sum(t => t.Jogos));
    }

    [Fact]
    public void Jogos_sem_placar_do_intervalo_ficam_de_fora()
    {
        Partida[] partidas = [Jogo(1, Bahia.Id, 2, Vitoria.Id, 0), Agendado(2, Bahia.Id, Sport.Id)];

        var resultado = CalculadoraTempos.Calcular(Times, partidas);

        Assert.All(resultado.Times, t => Assert.Equal(0, t.Jogos));
        Assert.False(resultado.TemFaixas);
    }

    [Fact]
    public void Ordena_por_quem_mais_ganha_pontos_depois_do_intervalo()
    {
        Partida[] partidas = [JogoComIntervalo(1, Vitoria.Id, Sport.Id, 2, 0, 0, 0)];

        var ordem = CalculadoraTempos.Calcular(Times, partidas).Times.Select(t => t.Time).ToList();

        Assert.Equal(Vitoria, ordem[0]);
        Assert.Equal(Sport, ordem[^1]);
    }

    [Theory]
    [InlineData(1, null, 0)]
    [InlineData(15, null, 0)]
    [InlineData(16, null, 1)]
    [InlineData(45, null, 2)]
    [InlineData(45, 3, 2)]
    [InlineData(46, null, 3)]
    [InlineData(75, null, 4)]
    [InlineData(90, 5, 5)]
    public void Classifica_o_minuto_na_faixa_certa(int minuto, int? acrescimo, int faixa)
    {
        Assert.Equal(faixa, CalculadoraTempos.FaixaDoMinuto(minuto, acrescimo));
    }

    [Fact]
    public void Soma_gols_marcados_e_sofridos_por_faixa_quando_ha_minutos()
    {
        var partida = Jogo(1, Bahia.Id, 2, Vitoria.Id, 1);
        Gol[] gols =
        [
            new(partida.Id, 10, null, Bahia.Id, TipoGol.Normal),
            new(partida.Id, 45, 2, Vitoria.Id, TipoGol.Penalti),
            new(partida.Id, 88, null, Bahia.Id, TipoGol.Contra),
        ];

        var resultado = CalculadoraTempos.Calcular(Times, [partida], gols);
        var bahia = resultado.Times.Single(t => t.Time == Bahia).Faixas!;
        var vitoria = resultado.Times.Single(t => t.Time == Vitoria).Faixas!;

        Assert.True(resultado.TemFaixas);
        Assert.Equal([1, 0, 0, 0, 0, 1], bahia.Marcados);
        Assert.Equal([0, 0, 1, 0, 0, 0], bahia.Sofridos);
        Assert.Equal(bahia.Marcados, vitoria.Sofridos);
        Assert.Null(resultado.Times.Single(t => t.Time == Sport).Faixas);
    }

    [Fact]
    public void Partida_no_segundo_tempo_desconta_o_placar_do_intervalo()
    {
        var partida = JogoComIntervalo(1, Bahia.Id, Vitoria.Id, 3, 2, 1, 2);

        var segundo = partida.NoTempo(Tempo.SegundoTempo)!;

        Assert.Equal((2, 0), (segundo.GolsMandante, segundo.GolsVisitante));
        Assert.Null(Jogo(1, Bahia.Id, 1, Vitoria.Id, 0).NoTempo(Tempo.PrimeiroTempo));
    }
}
