namespace Sumula.Core.Modelos;

/// <summary>Foto de um jogador com licença livre, com o crédito que a licença exige.</summary>
/// <param name="Url">Endereço da imagem (miniatura do Wikimedia Commons).</param>
/// <param name="Pagina">Página do arquivo, com autor e licença completos.</param>
public sealed record FotoJogador(string Url, string Pagina, string? Autor, string? Licenca);
