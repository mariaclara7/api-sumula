using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class SimuladorCampeonatoTests
{
    private static readonly Time[] Times = [Time(1, "Bahia"), Time(2, "Ceará"), Time(3, "Santos"), Time(4, "Sport")];

    [Fact]
    public void Campeonato_encerrado_reproduz_a_tabela_final()
    {
        Partida[] partidas =
        [
            Jogo(1, 1, 3, 2, 0),
            Jogo(1, 3, 1, 4, 1),
            Jogo(2, 1, 2, 3, 0),
            Jogo(2, 2, 0, 4, 1),
        ];

        var resultado = SimuladorCampeonato.Simular(Times, partidas, simulacoes: 200);
        var bahia = resultado.Times.Single(t => t.Time.Id == 1);

        Assert.Equal(0, resultado.PartidasRestantes);
        Assert.Equal(1.0, bahia.Posicoes[0]);
        Assert.Equal(6, bahia.PontosEsperados);
        Assert.Equal(1, bahia.PosicaoMedia);
    }

    [Fact]
    public void Probabilidades_somam_um_por_time_e_por_posicao()
    {
        Partida[] partidas =
        [
            Jogo(1, 1, 2, 2, 1),
            Jogo(1, 3, 0, 4, 0),
            Agendado(2, 1, 3),
            Agendado(2, 2, 4),
            Agendado(3, 1, 4),
            Agendado(3, 2, 3),
        ];

        var resultado = SimuladorCampeonato.Simular(Times, partidas, simulacoes: 2_000);

        Assert.Equal(4, resultado.PartidasRestantes);
        Assert.All(resultado.Times, t => Assert.Equal(1.0, t.Posicoes.Sum(), precision: 9));
        for (var posicao = 0; posicao < Times.Length; posicao++)
            Assert.Equal(1.0, resultado.Times.Sum(t => t.Posicoes[posicao]), precision: 9);
    }

    [Fact]
    public void Lider_com_vantagem_impossivel_de_alcancar_ja_e_campeao()
    {
        var partidas = new List<Partida>();
        for (var rodada = 1; rodada <= 3; rodada++)
        {
            partidas.Add(Jogo(rodada, 1, 3, 2, 0));
            partidas.Add(Jogo(rodada, 1, 3, 3, 0));
            partidas.Add(Jogo(rodada, 1, 3, 4, 0));
        }

        // 27 pontos contra no máximo 3 dos outros, com só um jogo restante para cada.
        partidas.Add(Agendado(4, 2, 3));
        partidas.Add(Agendado(4, 4, 1));

        var resultado = SimuladorCampeonato.Simular(Times, partidas, simulacoes: 1_000);

        Assert.Equal(1.0, resultado.Times.Single(t => t.Time.Id == 1).Posicoes[0]);
    }

    [Fact]
    public void Time_que_vence_mais_tem_mais_pontos_esperados_e_fica_a_frente()
    {
        var partidas = new List<Partida>();
        for (var rodada = 1; rodada <= 6; rodada++)
            partidas.Add(Jogo(rodada, rodada % 2 == 0 ? 1 : 2, rodada % 2 == 0 ? 3 : 0, rodada % 2 == 0 ? 2 : 1, rodada % 2 == 0 ? 0 : 3));
        partidas.AddRange(Enumerable.Range(7, 6).Select(r => Agendado(r, 1, 2)));

        var resultado = SimuladorCampeonato.Simular([Times[0], Times[1]], partidas, simulacoes: 2_000);

        Assert.Equal(1, resultado.Times[0].Time.Id);
        Assert.True(resultado.Times[0].PontosEsperados > resultado.Times[1].PontosEsperados);
    }

    [Fact]
    public void Mesma_semente_gera_o_mesmo_resultado()
    {
        Partida[] partidas = [Jogo(1, 1, 1, 2, 0), Agendado(2, 2, 1), Agendado(2, 3, 4)];

        var primeira = SimuladorCampeonato.Simular(Times, partidas, simulacoes: 500, semente: 42);
        var segunda = SimuladorCampeonato.Simular(Times, partidas, simulacoes: 500, semente: 42);

        Assert.Equal(
            primeira.Times.SelectMany(t => t.Posicoes),
            segunda.Times.SelectMany(t => t.Posicoes));
    }

    [Fact]
    public void Sorteio_de_poisson_respeita_a_media()
    {
        var aleatorio = new Random(7);
        var media = Enumerable.Range(0, 50_000).Average(_ => SimuladorCampeonato.SortearPoisson(1.4, aleatorio));

        Assert.InRange(media, 1.37, 1.43);
    }
}
