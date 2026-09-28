using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class CalculadoraConfrontoTests
{
    [Fact]
    public void Resume_so_os_jogos_entre_os_dois_times()
    {
        Partida[] partidas =
        [
            Jogo(1, 1, 2, 2, 1),
            Jogo(2, 1, 5, 3, 0),
            Jogo(20, 2, 0, 1, 0),
            Agendado(38, 1, 2),
        ];

        var resumo = CalculadoraConfronto.Resumir(1, 2, partidas);

        Assert.Equal((2, 1, 1, 0, 2, 1), (resumo.Jogos, resumo.VitoriasA, resumo.Empates, resumo.VitoriasB, resumo.GolsA, resumo.GolsB));
        Assert.Equal(3, resumo.Partidas.Count); // inclui o jogo agendado
    }

    [Fact]
    public void A_ordem_dos_times_inverte_o_resumo()
    {
        Partida[] partidas = [Jogo(1, 1, 2, 2, 1)];

        var resumo = CalculadoraConfronto.Resumir(2, 1, partidas);

        Assert.Equal((0, 1, 1, 2), (resumo.VitoriasA, resumo.VitoriasB, resumo.GolsA, resumo.GolsB));
    }
}
