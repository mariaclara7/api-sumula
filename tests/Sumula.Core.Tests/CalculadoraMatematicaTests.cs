using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class CalculadoraMatematicaTests
{
    private static readonly Time[] Times = [Time(1, "Bahia"), Time(2, "Ceará"), Time(3, "Santos"), Time(4, "Sport")];

    [Fact]
    public void Lider_inalcancavel_ja_e_campeao()
    {
        var partidas = new List<Partida>();
        for (var rodada = 1; rodada <= 3; rodada++)
        {
            partidas.Add(Jogo(rodada, 1, 2, 2, 0));
            partidas.Add(Jogo(rodada, 1, 2, 3, 0));
            partidas.Add(Jogo(rodada, 1, 2, 4, 0));
        }

        partidas.Add(Agendado(4, 2, 3));

        var situacao = CalculadoraMatematica.Calcular(Times, partidas);
        var bahia = situacao.Single(s => s.Time.Id == 1);
        var ceara = situacao.Single(s => s.Time.Id == 2);

        Assert.Equal((1, 1), (bahia.MelhorPosicaoPossivel, bahia.PiorPosicaoPossivel));
        Assert.Equal(1, ceara.JogosRestantes);
        Assert.Equal(3, ceara.PontosMaximos);
        Assert.True(ceara.MelhorPosicaoPossivel > 1);
    }

    [Fact]
    public void Com_tudo_em_aberto_todos_podem_terminar_em_qualquer_lugar()
    {
        Partida[] partidas = [Agendado(1, 1, 2), Agendado(1, 3, 4), Agendado(2, 1, 3), Agendado(2, 2, 4)];

        var situacao = CalculadoraMatematica.Calcular(Times, partidas);

        Assert.All(situacao, s => Assert.Equal((1, 4), (s.MelhorPosicaoPossivel, s.PiorPosicaoPossivel)));
    }

    [Fact]
    public void Pontos_para_garantir_superam_o_maximo_do_k_esimo_adversario()
    {
        // Pontos atuais: Bahia 6, Ceará 3, Santos 3, Sport 0. Cada um tem 1 jogo restante.
        Partida[] partidas =
        [
            Jogo(1, 1, 1, 4, 0),
            Jogo(1, 2, 1, 3, 0),
            Jogo(2, 1, 1, 2, 0),
            Jogo(2, 3, 1, 4, 0),
            Agendado(3, 1, 3),
            Agendado(3, 2, 4),
        ];

        var bahia = CalculadoraMatematica.Calcular(Times, partidas).Single(s => s.Time.Id == 1);

        // Máximos dos outros: Ceará 6, Santos 6, Sport 3.
        Assert.Equal([7, 7, 4, 0], bahia.PontosParaGarantir);
        Assert.Equal(9, bahia.PontosMaximos);
    }
}
