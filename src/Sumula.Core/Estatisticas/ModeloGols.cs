using Sumula.Core.Modelos;

namespace Sumula.Core.Estatisticas;

/// <summary>
/// Quantos gols cada time deve fazer em um jogo, a partir do ataque e da defesa na temporada
/// e da média de gols de mandantes e visitantes. Usado pelo simulador e pelo palpite de cada jogo.
/// </summary>
/// <remarks>
/// Força de ataque = gols marcados por jogo ÷ média da liga (defesa: gols sofridos). Com poucos jogos,
/// as forças são puxadas para a média, para um início de campeonato não dominar a previsão.
/// </remarks>
public sealed class ModeloGols
{
    /// <summary>Peso, em jogos, da média da liga nas forças de cada time.</summary>
    private const double JogosDeRegularizacao = 5;

    // Médias históricas aproximadas do Brasileirão, usadas antes da primeira rodada.
    private const double GolsMandantePadrao = 1.4;
    private const double GolsVisitantePadrao = 1.0;

    private readonly Dictionary<int, (double Ataque, double Defesa)> _forcas;

    private ModeloGols(double mediaMandante, double mediaVisitante, Dictionary<int, (double, double)> forcas)
    {
        MediaMandante = mediaMandante;
        MediaVisitante = mediaVisitante;
        _forcas = forcas;
    }

    public double MediaMandante { get; }
    public double MediaVisitante { get; }

    public static ModeloGols Estimar(IReadOnlyCollection<Time> times, IReadOnlyCollection<Partida> partidas)
    {
        var jogadas = partidas.Where(p => p.TemResultado).ToList();
        var mediaMandante = jogadas.Count > 0 ? jogadas.Average(p => (double)p.GolsMandante!.Value) : GolsMandantePadrao;
        var mediaVisitante = jogadas.Count > 0 ? jogadas.Average(p => (double)p.GolsVisitante!.Value) : GolsVisitantePadrao;

        // Uma liga sem gols deixaria a média zerada e todas as forças indefinidas.
        mediaMandante = Math.Max(mediaMandante, 0.2);
        mediaVisitante = Math.Max(mediaVisitante, 0.2);
        var mediaPorTime = (mediaMandante + mediaVisitante) / 2;

        double Forca(int gols, int jogos) =>
            (gols + JogosDeRegularizacao * mediaPorTime) / (jogos + JogosDeRegularizacao) / mediaPorTime;

        var forcas = CalculadoraClassificacao.Calcular(times, partidas)
            .ToDictionary(l => l.Time.Id, l => (Forca(l.GolsPro, l.Jogos), Forca(l.GolsContra, l.Jogos)));

        return new ModeloGols(mediaMandante, mediaVisitante, forcas);
    }

    /// <summary>Gols esperados de cada lado. Um time fora da tabela é tratado como médio.</summary>
    public (double Mandante, double Visitante) GolsEsperados(int mandanteId, int visitanteId)
    {
        var (ataqueMandante, defesaMandante) = _forcas.GetValueOrDefault(mandanteId, (1, 1));
        var (ataqueVisitante, defesaVisitante) = _forcas.GetValueOrDefault(visitanteId, (1, 1));

        return (MediaMandante * ataqueMandante * defesaVisitante, MediaVisitante * ataqueVisitante * defesaMandante);
    }
}
