using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class CalculadoraEvolucaoTests
{
    [Fact]
    public void Registra_posicao_e_pontos_ao_fim_de_cada_rodada()
    {
        Time[] times = [Time(1, "Bahia"), Time(2, "Vasco")];
        Partida[] partidas =
        [
            Jogo(1, 1, 1, 2, 0),
            Jogo(2, 2, 3, 1, 0),
            Agendado(3, 1, 2),
        ];

        var evolucao = CalculadoraEvolucao.Calcular(times, partidas);

        Assert.Equal([new PontoEvolucao(1, 1, 3), new PontoEvolucao(2, 2, 3)], evolucao[1]);
        Assert.Equal([new PontoEvolucao(1, 2, 0), new PontoEvolucao(2, 1, 3)], evolucao[2]);
    }
}
