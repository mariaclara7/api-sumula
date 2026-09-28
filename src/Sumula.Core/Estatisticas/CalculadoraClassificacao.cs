using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public static class CalculadoraClassificacao
{
    public const int QuantidadeUltimosResultados = 5;

    /// <summary>
    /// Monta a tabela seguindo os critérios de desempate do Brasileirão:
    /// pontos, vitórias, saldo de gols, gols pró e confronto direto (só quando o empate é entre dois clubes).
    /// Cartões não estão na fonte de dados, então o último critério é o nome do time.
    /// </summary>
    public static IReadOnlyList<LinhaClassificacao> Calcular(
        IReadOnlyCollection<Time> times,
        IEnumerable<Partida> partidas,
        Filtro? filtro = null)
    {
        filtro ??= Filtro.Todos;

        var consideradas = partidas
            .Where(p => p.TemResultado && filtro.IncluiRodada(p))
            .OrderBy(p => p.Data)
            .ToList();

        var linhas = times
            .Select(time => Acumular(time, consideradas.Where(p => filtro.IncluiParaTime(p, time.Id))))
            .ToList();

        var ordenadas = linhas
            .GroupBy(l => (l.Pontos, l.Vitorias, l.Saldo, l.GolsPro))
            .OrderByDescending(g => g.Key.Pontos)
            .ThenByDescending(g => g.Key.Vitorias)
            .ThenByDescending(g => g.Key.Saldo)
            .ThenByDescending(g => g.Key.GolsPro)
            .SelectMany(g => DesempatarGrupo(g.ToList(), consideradas));

        return ordenadas.Select((linha, indice) => linha with { Posicao = indice + 1 }).ToList();
    }

    private static LinhaClassificacao Acumular(Time time, IEnumerable<Partida> partidasDoTime)
    {
        int vitorias = 0, empates = 0, derrotas = 0, golsPro = 0, golsContra = 0;
        var resultados = new List<Resultado>();

        foreach (var partida in partidasDoTime)
        {
            golsPro += partida.GolsDe(time.Id);
            golsContra += partida.GolsContra(time.Id);

            var resultado = partida.ResultadoPara(time.Id)!.Value;
            resultados.Add(resultado);
            switch (resultado)
            {
                case Resultado.Vitoria: vitorias++; break;
                case Resultado.Empate: empates++; break;
                default: derrotas++; break;
            }
        }

        return new LinhaClassificacao(
            Posicao: 0,
            Time: time,
            Pontos: vitorias * 3 + empates,
            Jogos: resultados.Count,
            Vitorias: vitorias,
            Empates: empates,
            Derrotas: derrotas,
            GolsPro: golsPro,
            GolsContra: golsContra,
            UltimosResultados: resultados.TakeLast(QuantidadeUltimosResultados).ToList());
    }

    private static IEnumerable<LinhaClassificacao> DesempatarGrupo(
        List<LinhaClassificacao> grupo,
        IReadOnlyList<Partida> partidas)
    {
        if (grupo.Count != 2)
            return grupo.OrderBy(l => l.Time.Nome, StringComparer.CurrentCulture);

        var confronto = CalculadoraConfronto.Resumir(grupo[0].Time.Id, grupo[1].Time.Id, partidas);
        var pontosA = confronto.VitoriasA * 3 + confronto.Empates;
        var pontosB = confronto.VitoriasB * 3 + confronto.Empates;

        if (pontosA == pontosB)
            return grupo.OrderBy(l => l.Time.Nome, StringComparer.CurrentCulture);

        return pontosA > pontosB ? grupo : [grupo[1], grupo[0]];
    }
}
