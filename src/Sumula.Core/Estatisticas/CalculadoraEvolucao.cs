using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public sealed record PontoEvolucao(int Rodada, int Posicao, int Pontos);

public static class CalculadoraEvolucao
{
    /// <summary>
    /// Posição e pontos de cada time ao fim de cada rodada. Jogos adiados entram
    /// na rodada a que pertencem, como na tabela oficial "por rodada".
    /// </summary>
    public static IReadOnlyDictionary<int, IReadOnlyList<PontoEvolucao>> Calcular(
        IReadOnlyCollection<Time> times,
        IReadOnlyCollection<Partida> partidas)
    {
        var ultimaRodada = partidas.Where(p => p.TemResultado).Select(p => p.Rodada).DefaultIfEmpty(0).Max();
        var evolucao = times.ToDictionary(t => t.Id, _ => new List<PontoEvolucao>());

        for (var rodada = 1; rodada <= ultimaRodada; rodada++)
        {
            var tabela = CalculadoraClassificacao.Calcular(times, partidas, new Filtro(RodadaFinal: rodada));
            foreach (var linha in tabela)
                evolucao[linha.Time.Id].Add(new PontoEvolucao(rodada, linha.Posicao, linha.Pontos));
        }

        return evolucao.ToDictionary(par => par.Key, par => (IReadOnlyList<PontoEvolucao>)par.Value);
    }
}
