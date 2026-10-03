using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sumula.Coletor.FootballData;
using Sumula.Core.Fotos;

namespace Sumula.Coletor.Fotos;

/// <summary>
/// Procura fotos com licença livre para os artilheiros e grava em <c>dados/fotos-jogadores.json</c>.
/// O jogador do football-data.org é achado no Wikidata pelo nome e confirmado pela data de nascimento;
/// a foto é a imagem principal do item (P18), que fica no Wikimedia Commons.
/// </summary>
public class BuscadorFotos(
    ClienteFootballData footballData,
    ClienteWikimedia wikimedia,
    IOptions<OpcoesColeta> opcoes,
    TimeProvider relogio,
    ILogger<BuscadorFotos> logger)
{
    /// <summary>Jogador procurado e não achado só é procurado de novo depois desse tempo.</summary>
    public static readonly TimeSpan IntervaloNovaBusca = TimeSpan.FromDays(30);

    public async Task ExecutarAsync(string caminhoArquivo, CancellationToken ct)
    {
        var arquivo = File.Exists(caminhoArquivo)
            ? ArquivoFotos.Ler(await File.ReadAllTextAsync(caminhoArquivo, ct))
            : new ArquivoFotos();

        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        var temporada = opcoes.Value.Temporada ?? relogio.GetUtcNow().Year;
        var competicoes = opcoes.Value.Competicoes.Select(c => c.ToUpperInvariant()).Distinct().DefaultIfEmpty("BSA");

        var jogadores = new Dictionary<int, JogadorFd>();
        foreach (var competicao in competicoes)
        {
            var artilharia = await footballData.ObterArtilhariaAsync(competicao, temporada, ct);
            foreach (var artilheiro in artilharia.Scorers)
                jogadores.TryAdd(artilheiro.Player.Id, artilheiro.Player);
        }

        int comFoto = 0, semFoto = 0, pulados = 0;
        foreach (var jogador in jogadores.Values)
        {
            if (!PrecisaBuscar(arquivo, jogador.Id, hoje))
            {
                pulados++;
                continue;
            }

            var registro = await BuscarAsync(jogador, hoje, ct);
            arquivo.Jogadores[jogador.Id] = registro;
            if (registro.Foto is null) semFoto++; else comFoto++;
        }

        await File.WriteAllTextAsync(caminhoArquivo, arquivo.ParaJson(), ct);
        logger.LogInformation(
            "Fotos: {ComFoto} encontradas, {SemFoto} sem foto livre, {Pulados} já conhecidos. Total no arquivo: {Total}.",
            comFoto, semFoto, pulados, arquivo.Jogadores.Count);
    }

    /// <summary>Entradas manuais e fotos já achadas ficam; quem não tinha foto é procurado de novo de tempos em tempos.</summary>
    public static bool PrecisaBuscar(ArquivoFotos arquivo, int jogadorId, DateOnly hoje) =>
        !arquivo.Jogadores.TryGetValue(jogadorId, out var registro) ||
        (!registro.Manual && registro.Foto is null && registro.VerificadoEm.AddDays(IntervaloNovaBusca.Days) <= hoje);

    private async Task<RegistroFoto> BuscarAsync(JogadorFd jogador, DateOnly hoje, CancellationToken ct)
    {
        if (jogador.DateOfBirth is not { } nascimento)
        {
            logger.LogInformation("{Nome}: sem data de nascimento no football-data.org, não dá para confirmar.", jogador.Name);
            return new RegistroFoto(jogador.Name, null, null, hoje);
        }

        // O nome curto ("Hulk") acha os mais conhecidos; o completo ajuda nos nomes comuns.
        var termos = new[] { jogador.Name, $"{jogador.FirstName} {jogador.LastName}".Trim() }
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var ids = new List<string>();
        foreach (var termo in termos)
        {
            ids.AddRange(await wikimedia.BuscarJogadoresAsync(termo, ct));
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
        }

        var entidades = await wikimedia.ObterEntidadesAsync(ids.Distinct().Take(50), ct);
        var candidatos = Escolher(entidades, nascimento);
        if (candidatos.Count != 1)
        {
            logger.LogInformation("{Nome}: {Quantidade} candidatos no Wikidata com a mesma data de nascimento.",
                jogador.Name, candidatos.Count);
            return new RegistroFoto(jogador.Name, null, null, hoje);
        }

        var item = candidatos[0];
        var foto = item.Imagem is null ? null : await wikimedia.ObterFotoAsync(item.Imagem, ct);
        logger.LogInformation("{Nome}: {Item} {Resultado}.", jogador.Name, item.Id, foto is null ? "sem foto" : "com foto");
        return new RegistroFoto(jogador.Name, item.Id, foto, hoje);
    }

    /// <summary>Só aceita quem nasceu no mesmo dia: o nome sozinho erra com homônimos ("Pedro", "Gabriel").</summary>
    public static IReadOnlyList<EntidadeWikidata> Escolher(IEnumerable<EntidadeWikidata> entidades, DateOnly nascimento) =>
        entidades.Where(e => e.Nascimento == nascimento).DistinctBy(e => e.Id).ToList();
}
