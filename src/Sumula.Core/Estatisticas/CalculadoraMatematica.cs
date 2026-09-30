using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

/// <param name="MelhorPosicaoPossivel">A posição mais alta que o time ainda pode alcançar.</param>
/// <param name="PiorPosicaoPossivel">A posição mais baixa em que o time ainda pode terminar.</param>
/// <param name="PontosParaGarantir">
/// Índice k-1: total de pontos que garante terminar entre os k primeiros, aconteça o que acontecer nos outros
/// jogos. Pode passar de <see cref="PontosMaximos"/>: nesse caso o time depende de tropeços dos outros.
/// </param>
public sealed record SituacaoMatematica(
    Time Time,
    int Posicao,
    int Pontos,
    int JogosRestantes,
    int PontosMaximos,
    int MelhorPosicaoPossivel,
    int PiorPosicaoPossivel,
    IReadOnlyList<int> PontosParaGarantir);

/// <summary>
/// O que já está decidido "matematicamente": quem ainda pode ser campeão, quem já garantiu vaga,
/// quem já caiu e quantos pontos bastam para cada posição.
/// </summary>
/// <remarks>
/// O cálculo é conservador: considera que cada adversário pode ganhar todos os jogos que faltam, mesmo
/// quando dois deles ainda se enfrentam, e que um empate em pontos pode ser decidido contra o time.
/// Por isso "garantido" nunca é dito antes da hora, mas pode ser dito uma ou duas rodadas depois de
/// uma conta mais fina, que considerasse os confrontos entre os adversários.
/// </remarks>
public static class CalculadoraMatematica
{
    public static IReadOnlyList<SituacaoMatematica> Calcular(IReadOnlyCollection<Time> times, IReadOnlyCollection<Partida> partidas)
    {
        var tabela = CalculadoraClassificacao.Calcular(times, partidas);
        var restantes = partidas
            .Where(p => !p.TemResultado && p.Status != StatusPartida.Cancelada)
            .SelectMany(p => new[] { p.MandanteId, p.VisitanteId })
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var situacoes = tabela.Select(l =>
        {
            var jogosRestantes = restantes.GetValueOrDefault(l.Time.Id);
            return (Linha: l, JogosRestantes: jogosRestantes, Maximo: l.Pontos + 3 * jogosRestantes);
        }).ToList();

        return situacoes.Select(atual =>
        {
            var outros = situacoes.Where(s => s.Linha.Time.Id != atual.Linha.Time.Id).ToList();

            // Pode terminar à frente de mim todo adversário que consegue chegar aos meus pontos atuais.
            var pior = 1 + outros.Count(o => o.Maximo >= atual.Linha.Pontos);
            // Com certeza fica à frente quem já tem mais pontos do que o meu máximo.
            var melhor = 1 + outros.Count(o => o.Linha.Pontos > atual.Maximo);

            // Para terminar entre os k primeiros, basta superar o máximo do k-ésimo adversário mais forte:
            // aí no máximo k-1 deles conseguem me alcançar.
            var maximosOutros = outros.Select(o => o.Maximo).OrderByDescending(m => m).ToList();
            var paraGarantir = Enumerable.Range(1, situacoes.Count)
                .Select(k => k <= maximosOutros.Count ? maximosOutros[k - 1] + 1 : 0)
                .ToList();

            return new SituacaoMatematica(
                atual.Linha.Time,
                atual.Linha.Posicao,
                atual.Linha.Pontos,
                atual.JogosRestantes,
                atual.Maximo,
                melhor,
                pior,
                paraGarantir);
        }).ToList();
    }
}
