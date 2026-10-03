using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sumula.Core.Modelos;

namespace Sumula.Core.Fotos;

/// <summary>Um jogador no arquivo de fotos.</summary>
/// <param name="Wikidata">Item do jogador no Wikidata, quando encontrado.</param>
/// <param name="Foto">Nulo quando o jogador não tem foto livre (ou não foi encontrado).</param>
/// <param name="Manual">Preenchido à mão: a busca automática nunca altera.</param>
public sealed record RegistroFoto(string Nome, string? Wikidata, FotoJogador? Foto, DateOnly VerificadoEm, bool Manual = false);

/// <summary>
/// O arquivo <c>dados/fotos-jogadores.json</c>: fotos por id do jogador no football-data.org.
/// Gerado pelo comando "fotos" do coletor e versionado no repositório, para dar para revisar e corrigir à mão.
/// </summary>
public sealed class ArquivoFotos
{
    public SortedDictionary<int, RegistroFoto> Jogadores { get; init; } = [];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // Acentos ficam legíveis no arquivo ("Ã" em vez de "Ã").
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    };

    public static ArquivoFotos Ler(string json) =>
        JsonSerializer.Deserialize<ArquivoFotos>(json, Json) ?? new ArquivoFotos();

    public static ArquivoFotos Ler(Stream json) =>
        JsonSerializer.Deserialize<ArquivoFotos>(json, Json) ?? new ArquivoFotos();

    public string ParaJson() => JsonSerializer.Serialize(this, Json) + "\n";

    public FotoJogador? Foto(int jogadorId) => Jogadores.TryGetValue(jogadorId, out var registro) ? registro.Foto : null;
}
