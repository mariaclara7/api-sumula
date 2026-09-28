using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using static Sumula.Core.Tests.Construtor;

namespace Sumula.Core.Tests;

public class CalculadoraClassificacaoTests
{
    private static readonly Time Flamengo = Time(1, "Flamengo");
    private static readonly Time Palmeiras = Time(2, "Palmeiras");
    private static readonly Time Botafogo = Time(3, "Botafogo");
    private static readonly Time Corinthians = Time(4, "Corinthians");
    private static readonly Time[] Times = [Flamengo, Palmeiras, Botafogo, Corinthians];

    [Fact]
    public void Soma_pontos_jogos_e_gols()
    {
        Partida[] partidas =
        [
            Jogo(1, Flamengo.Id, 2, Palmeiras.Id, 0),
            Jogo(1, Botafogo.Id, 1, Corinthians.Id, 1),
            Jogo(2, Palmeiras.Id, 3, Botafogo.Id, 1),
        ];

        var tabela = CalculadoraClassificacao.Calcular(Times, partidas);

        var flamengo = tabela.Single(l => l.Time == Flamengo);
        Assert.Equal((3, 1, 1, 0, 0, 2, 0, 2), (flamengo.Pontos, flamengo.Jogos, flamengo.Vitorias,
            flamengo.Empates, flamengo.Derrotas, flamengo.GolsPro, flamengo.GolsContra, flamengo.Saldo));

        var palmeiras = tabela.Single(l => l.Time == Palmeiras);
        Assert.Equal((3, 2, 3, 3), (palmeiras.Pontos, palmeiras.Jogos, palmeiras.GolsPro, palmeiras.GolsContra));
        Assert.Equal(50.0, palmeiras.Aproveitamento);
    }

    [Fact]
    public void Inclui_times_sem_jogos_e_ignora_partidas_sem_resultado()
    {
        Partida[] partidas = [Agendado(1, Flamengo.Id, Palmeiras.Id)];

        var tabela = CalculadoraClassificacao.Calcular(Times, partidas);

        Assert.Equal(4, tabela.Count);
        Assert.All(tabela, l => Assert.Equal(0, l.Jogos));
        Assert.Equal([1, 2, 3, 4], tabela.Select(l => l.Posicao));
    }

    [Fact]
    public void Desempata_por_vitorias_depois_saldo_depois_gols_pro()
    {
        Partida[] partidas =
        [
            // Flamengo: 1V 2D = 3 pts. Palmeiras: 3E = 3 pts. Vitórias decidem.
            Jogo(1, Flamengo.Id, 1, Botafogo.Id, 0),
            Jogo(2, Flamengo.Id, 0, Corinthians.Id, 1),
            Jogo(3, Flamengo.Id, 0, Botafogo.Id, 1),
            Jogo(1, Palmeiras.Id, 0, Corinthians.Id, 0),
            Jogo(2, Palmeiras.Id, 1, Botafogo.Id, 1),
            Jogo(3, Palmeiras.Id, 2, Corinthians.Id, 2),
        ];

        var tabela = CalculadoraClassificacao.Calcular(Times, partidas);

        Assert.True(Posicao(tabela, Flamengo) < Posicao(tabela, Palmeiras));
    }

    [Fact]
    public void Com_tudo_igual_entre_dois_times_vale_o_confronto_direto()
    {
        // Botafogo e Palmeiras terminam iguais em pontos, vitórias, saldo e gols pró,
        // mas o Botafogo venceu o jogo entre os dois.
        Partida[] partidas =
        [
            Jogo(1, Botafogo.Id, 1, Palmeiras.Id, 0),
            Jogo(2, Palmeiras.Id, 1, Flamengo.Id, 0),
            Jogo(2, Botafogo.Id, 0, Corinthians.Id, 1),
        ];

        var tabela = CalculadoraClassificacao.Calcular(Times, partidas);
        var botafogo = tabela.Single(l => l.Time == Botafogo);
        var palmeiras = tabela.Single(l => l.Time == Palmeiras);

        Assert.Equal((botafogo.Pontos, botafogo.Vitorias, botafogo.Saldo, botafogo.GolsPro),
            (palmeiras.Pontos, palmeiras.Vitorias, palmeiras.Saldo, palmeiras.GolsPro));
        Assert.True(botafogo.Posicao < palmeiras.Posicao);
    }

    [Fact]
    public void Primeiro_e_segundo_turno_consideram_so_as_rodadas_de_cada_turno()
    {
        // 4 times => 3 rodadas por turno.
        Partida[] partidas =
        [
            Jogo(1, Flamengo.Id, 1, Palmeiras.Id, 0),
            Jogo(3, Flamengo.Id, 2, Botafogo.Id, 0),
            Jogo(4, Palmeiras.Id, 3, Flamengo.Id, 0),
            Jogo(6, Botafogo.Id, 1, Flamengo.Id, 0),
        ];

        var primeiro = CalculadoraClassificacao.Calcular(Times, partidas, Filtro.DoRecorte(Recorte.PrimeiroTurno, Times.Length));
        var segundo = CalculadoraClassificacao.Calcular(Times, partidas, Filtro.DoRecorte(Recorte.SegundoTurno, Times.Length));

        Assert.Equal(6, primeiro.Single(l => l.Time == Flamengo).Pontos);
        Assert.Equal(0, segundo.Single(l => l.Time == Flamengo).Pontos);
        Assert.Equal(2, segundo.Single(l => l.Time == Flamengo).Jogos);
    }

    [Fact]
    public void Filtra_jogos_em_casa_e_fora()
    {
        Partida[] partidas =
        [
            Jogo(1, Flamengo.Id, 3, Palmeiras.Id, 0),
            Jogo(2, Botafogo.Id, 2, Flamengo.Id, 1),
        ];

        var casa = CalculadoraClassificacao.Calcular(Times, partidas, new Filtro(Mando: Mando.Casa));
        var fora = CalculadoraClassificacao.Calcular(Times, partidas, new Filtro(Mando: Mando.Fora));

        Assert.Equal((3, 1), (casa.Single(l => l.Time == Flamengo).Pontos, casa.Single(l => l.Time == Flamengo).Jogos));
        Assert.Equal((0, 1), (fora.Single(l => l.Time == Flamengo).Pontos, fora.Single(l => l.Time == Flamengo).Jogos));
        Assert.Equal(0, casa.Single(l => l.Time == Palmeiras).Jogos);
    }

    [Fact]
    public void Ultimos_resultados_trazem_os_cinco_jogos_mais_recentes_em_ordem()
    {
        var partidas = new List<Partida>
        {
            Jogo(1, Flamengo.Id, 0, Palmeiras.Id, 1), // D (fica de fora)
            Jogo(2, Flamengo.Id, 1, Botafogo.Id, 0), // V
            Jogo(3, Flamengo.Id, 1, Corinthians.Id, 1), // E
            Jogo(4, Flamengo.Id, 2, Palmeiras.Id, 0), // V
            Jogo(5, Botafogo.Id, 2, Flamengo.Id, 0), // D
            Jogo(6, Corinthians.Id, 0, Flamengo.Id, 1), // V
        };

        var flamengo = CalculadoraClassificacao.Calcular(Times, partidas).Single(l => l.Time == Flamengo);

        Assert.Equal(
            [Resultado.Vitoria, Resultado.Empate, Resultado.Vitoria, Resultado.Derrota, Resultado.Vitoria],
            flamengo.UltimosResultados);
    }

    private static int Posicao(IReadOnlyList<LinhaClassificacao> tabela, Time time) =>
        tabela.Single(l => l.Time == time).Posicao;
}
