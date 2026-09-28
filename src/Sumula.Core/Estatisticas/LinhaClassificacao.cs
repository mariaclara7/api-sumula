using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

public sealed record LinhaClassificacao(
    int Posicao,
    Time Time,
    int Pontos,
    int Jogos,
    int Vitorias,
    int Empates,
    int Derrotas,
    int GolsPro,
    int GolsContra,
    IReadOnlyList<Resultado> UltimosResultados)
{
    public int Saldo => GolsPro - GolsContra;

    /// <summary>Percentual dos pontos disputados que o time conquistou (0 a 100).</summary>
    public double Aproveitamento => Jogos == 0 ? 0 : Math.Round(Pontos * 100.0 / (Jogos * 3), 1);
}
