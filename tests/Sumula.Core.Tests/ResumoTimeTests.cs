using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class ResumoTimeTests
{
    private static readonly Time[] Times = [Time(1, "Grêmio"), Time(2, "Inter"), Time(3, "Juventude")];

    [Fact]
    public void Separa_casa_fora_ultimas_e_proximas_partidas()
    {
        Partida[] partidas =
        [
            Jogo(1, 1, 2, 2, 0),
            Jogo(2, 3, 1, 1, 1),
            Agendado(3, 1, 3),
        ];

        var resumo = ResumoTime.Montar(1, Times, partidas)!;

        Assert.Equal(4, resumo.Geral.Pontos);
        Assert.Equal(3, resumo.Casa.Pontos);
        Assert.Equal(1, resumo.Fora.Pontos);
        Assert.Equal([2, 1], resumo.UltimasPartidas.Select(p => p.Rodada));
        Assert.Equal([3], resumo.ProximasPartidas.Select(p => p.Rodada));
        Assert.Equal(2, resumo.Evolucao.Count);
    }

    [Fact]
    public void Time_fora_da_competicao_retorna_nulo()
    {
        Assert.Null(ResumoTime.Montar(99, Times, []));
    }
}
