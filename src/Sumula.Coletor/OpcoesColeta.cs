namespace Sumula.Coletor;

public class OpcoesColeta
{
    /// <summary>Códigos do football-data.org. BSA = Campeonato Brasileiro Série A.</summary>
    /// <remarks>
    /// Sem valor padrão aqui: o binder de configuração soma listas ao valor inicial em vez de substituí-lo.
    /// </remarks>
    public string[] Competicoes { get; set; } = [];

    /// <summary>Se não for informada, usa o ano atual.</summary>
    public int? Temporada { get; set; }
}
