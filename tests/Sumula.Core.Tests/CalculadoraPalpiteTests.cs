using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class CalculadoraPalpiteTests
{
    [Fact]
    public void Chances_somam_um()
    {
        var palpite = CalculadoraPalpite.Calcular(1, (1.4, 1.1));

        Assert.Equal(1.0, palpite.VitoriaMandante + palpite.Empate + palpite.VitoriaVisitante, precision: 9);
    }

    [Fact]
    public void Times_iguais_tem_as_mesmas_chances_e_o_placar_mais_provavel_e_um_empate()
    {
        var palpite = CalculadoraPalpite.Calcular(1, (1.2, 1.2));

        Assert.Equal(palpite.VitoriaMandante, palpite.VitoriaVisitante, precision: 9);
        Assert.Equal(palpite.PlacarMaisProvavel.Mandante, palpite.PlacarMaisProvavel.Visitante);
    }

    [Fact]
    public void Quem_espera_mais_gols_e_favorito()
    {
        var palpite = CalculadoraPalpite.Calcular(1, (2.5, 0.5));

        Assert.True(palpite.VitoriaMandante > 0.7);
        Assert.True(palpite.PlacarMaisProvavel.Mandante > palpite.PlacarMaisProvavel.Visitante);
    }

    [Fact]
    public void So_da_palpite_para_jogos_sem_resultado()
    {
        Time[] times = [Time(1, "Bahia"), Time(2, "Vasco"), Time(3, "Santos")];
        // Gols equilibrados entre mandantes e visitantes, para o teste não depender do fator casa.
        Partida[] partidas =
        [
            Jogo(1, 1, 3, 2, 0),
            Jogo(2, 2, 0, 1, 3),
            Jogo(3, 3, 1, 2, 1),
            Jogo(4, 2, 1, 3, 1),
            Agendado(5, 1, 3),
            Agendado(6, 3, 1),
        ];

        var palpites = CalculadoraPalpite.Calcular(times, partidas);

        Assert.Equal([partidas[4].Id, partidas[5].Id], palpites.Select(p => p.PartidaId));
        // Bahia fez 6 gols e não sofreu nenhum: é favorito em casa e fora.
        Assert.True(palpites[0].VitoriaMandante > palpites[0].VitoriaVisitante);
        Assert.True(palpites[1].VitoriaVisitante > palpites[1].VitoriaMandante);
    }
}
