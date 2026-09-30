using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public sealed record ProbabilidadesTime(
    Time Time,
    int PosicaoAtual,
    int PontosAtuais,
    double PontosEsperados,
    double PosicaoMedia,
    // Chance de terminar em cada posição: índice 0 = 1º lugar. Soma 1.
    IReadOnlyList<double> Posicoes);

public sealed record ResultadoSimulacao(
    int Simulacoes,
    int PartidasRestantes,
    IReadOnlyList<ProbabilidadesTime> Times);

/// <summary>
/// Estima as chances de cada time terminar em cada posição simulando os jogos que faltam
/// milhares de vezes (método de Monte Carlo).
/// </summary>
/// <remarks>
/// O placar de cada jogo é sorteado por uma distribuição de Poisson, com a média de gols de cada lado
/// dada pelo <see cref="ModeloGols"/>.
/// </remarks>
public static class SimuladorCampeonato
{
    public const int SimulacoesPadrao = 10_000;

    public static ResultadoSimulacao Simular(
        IReadOnlyCollection<Time> times,
        IReadOnlyCollection<Partida> partidas,
        int simulacoes = SimulacoesPadrao,
        int semente = 0)
    {
        var tabela = CalculadoraClassificacao.Calcular(times, partidas);
        var quantidade = tabela.Count;
        if (quantidade == 0)
            return new ResultadoSimulacao(simulacoes, 0, []);

        var indicePorTime = tabela.Select((linha, indice) => (linha.Time.Id, indice)).ToDictionary(x => x.Id, x => x.indice);
        var modelo = ModeloGols.Estimar(times, partidas);
        var restantes = partidas
            .Where(p => !p.TemResultado && p.Status != StatusPartida.Cancelada)
            .Where(p => indicePorTime.ContainsKey(p.MandanteId) && indicePorTime.ContainsKey(p.VisitanteId))
            .Select(p =>
            {
                var (golsMandante, golsVisitante) = modelo.GolsEsperados(p.MandanteId, p.VisitanteId);
                return new JogoRestante(indicePorTime[p.MandanteId], indicePorTime[p.VisitanteId], golsMandante, golsVisitante);
            })
            .ToArray();

        var aleatorio = new Random(semente);
        var contagem = new int[quantidade, quantidade];
        var somaPontos = new long[quantidade];

        var pontos = new int[quantidade];
        var vitorias = new int[quantidade];
        var saldo = new int[quantidade];
        var golsPro = new int[quantidade];
        var sorteio = new double[quantidade];
        var ordem = new int[quantidade];

        for (var s = 0; s < simulacoes; s++)
        {
            for (var i = 0; i < quantidade; i++)
            {
                var linha = tabela[i];
                pontos[i] = linha.Pontos;
                vitorias[i] = linha.Vitorias;
                saldo[i] = linha.Saldo;
                golsPro[i] = linha.GolsPro;
                sorteio[i] = aleatorio.NextDouble();
                ordem[i] = i;
            }

            foreach (var jogo in restantes)
            {
                var golsMandante = SortearPoisson(jogo.MediaMandante, aleatorio);
                var golsVisitante = SortearPoisson(jogo.MediaVisitante, aleatorio);

                golsPro[jogo.Mandante] += golsMandante;
                golsPro[jogo.Visitante] += golsVisitante;
                saldo[jogo.Mandante] += golsMandante - golsVisitante;
                saldo[jogo.Visitante] += golsVisitante - golsMandante;

                if (golsMandante > golsVisitante)
                {
                    pontos[jogo.Mandante] += 3;
                    vitorias[jogo.Mandante]++;
                }
                else if (golsVisitante > golsMandante)
                {
                    pontos[jogo.Visitante] += 3;
                    vitorias[jogo.Visitante]++;
                }
                else
                {
                    pontos[jogo.Mandante]++;
                    pontos[jogo.Visitante]++;
                }
            }

            // Mesmos critérios da tabela. O confronto direto dá lugar a um sorteio, que
            // tem efeito desprezível nas probabilidades e deixa a simulação bem mais rápida.
            Array.Sort(ordem, (a, b) =>
                pontos[b] != pontos[a] ? pontos[b].CompareTo(pontos[a]) :
                vitorias[b] != vitorias[a] ? vitorias[b].CompareTo(vitorias[a]) :
                saldo[b] != saldo[a] ? saldo[b].CompareTo(saldo[a]) :
                golsPro[b] != golsPro[a] ? golsPro[b].CompareTo(golsPro[a]) :
                sorteio[a].CompareTo(sorteio[b]));

            for (var posicao = 0; posicao < quantidade; posicao++)
                contagem[ordem[posicao], posicao]++;

            for (var i = 0; i < quantidade; i++)
                somaPontos[i] += pontos[i];
        }

        var resultado = tabela.Select((linha, i) =>
        {
            var posicoes = Enumerable.Range(0, quantidade).Select(p => (double)contagem[i, p] / simulacoes).ToArray();
            return new ProbabilidadesTime(
                Time: linha.Time,
                PosicaoAtual: linha.Posicao,
                PontosAtuais: linha.Pontos,
                PontosEsperados: Math.Round((double)somaPontos[i] / simulacoes, 1),
                PosicaoMedia: Math.Round(posicoes.Select((p, indice) => p * (indice + 1)).Sum(), 2),
                Posicoes: posicoes);
        });

        return new ResultadoSimulacao(
            simulacoes,
            restantes.Length,
            resultado.OrderBy(t => t.PosicaoMedia).ThenBy(t => t.PosicaoAtual).ToList());
    }

    /// <summary>Algoritmo de Knuth; eficiente para médias pequenas como as de gols.</summary>
    public static int SortearPoisson(double media, Random aleatorio)
    {
        var limite = Math.Exp(-media);
        var produto = aleatorio.NextDouble();
        var gols = 0;
        while (produto > limite)
        {
            gols++;
            produto *= aleatorio.NextDouble();
        }

        return gols;
    }

    private sealed record JogoRestante(int Mandante, int Visitante, double MediaMandante, double MediaVisitante);
}
