using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class CalculadoraEstatisticasTests
{
    private static readonly Time[] Times = [Time(1, "Bahia"), Time(2, "Ceará"), Time(3, "Santos")];

    private static readonly Partida[] Partidas =
    [
        Jogo(1, 1, 2, 2, 0), // Bahia V, sem sofrer
        Jogo(2, 3, 1, 1, 1), // Bahia E, ambos marcam
        Jogo(3, 1, 3, 3, 2), // Bahia V, mais de 2,5
        Jogo(4, 2, 1, 1, 0), // Bahia D, não marcou
        Jogo(5, 1, 0, 3, 0), // Bahia E, sem sofrer, não marcou
        Agendado(6, 1, 2),
    ];

    [Fact]
    public void Monta_o_perfil_de_gols()
    {
        var bahia = CalculadoraEstatisticas.Calcular(Times, Partidas).Times.Single(t => t.Time.Id == 1).Gols;

        Assert.Equal(new PerfilGols(5, 6, 4, 2, 2, 1, 2), bahia);
    }

    [Fact]
    public void Calcula_sequencia_atual_e_maior_da_temporada()
    {
        var sequencias = CalculadoraEstatisticas.Calcular(Times, Partidas).Times.Single(t => t.Time.Id == 1).Sequencias
            .ToDictionary(s => s.Tipo);

        Assert.Equal((0, 1), (sequencias[TipoSequencia.Vitorias].Atual, sequencias[TipoSequencia.Vitorias].Maior));
        Assert.Equal((1, 3), (sequencias[TipoSequencia.Invencibilidade].Atual, sequencias[TipoSequencia.Invencibilidade].Maior));
        Assert.Equal((2, 2), (sequencias[TipoSequencia.SemVencer].Atual, sequencias[TipoSequencia.SemVencer].Maior));
        Assert.Equal((0, 3), (sequencias[TipoSequencia.Marcando].Atual, sequencias[TipoSequencia.Marcando].Maior));
        Assert.Equal((1, 1), (sequencias[TipoSequencia.SemSofrerGol].Atual, sequencias[TipoSequencia.SemSofrerGol].Maior));
    }

    [Fact]
    public void Resume_a_liga()
    {
        var liga = CalculadoraEstatisticas.Calcular(Times, Partidas).Liga;

        Assert.Equal(new ResumoLiga(5, 10, 3, 2, 0, 1, 2), liga);
    }
}
