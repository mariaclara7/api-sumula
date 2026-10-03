using Sumula.Core.Fotos;
using Sumula.Core.Modelos;

namespace Sumula.Api;

/// <summary>
/// Fotos dos jogadores, lidas de <c>dados/fotos-jogadores.json</c>. O arquivo vai embutido na API, então a
/// foto nova aparece na próxima publicação (o workflow "Fotos" já dispara o Coletor depois de atualizar).
/// </summary>
public sealed class CatalogoFotos
{
    private readonly ArquivoFotos arquivo;

    public CatalogoFotos()
    {
        using var json = typeof(CatalogoFotos).Assembly.GetManifestResourceStream("fotos-jogadores.json")
            ?? throw new InvalidOperationException("fotos-jogadores.json não foi embutido na API.");
        arquivo = ArquivoFotos.Ler(json);
    }

    public Artilheiro ComFoto(Artilheiro artilheiro) => artilheiro with { Foto = arquivo.Foto(artilheiro.JogadorId) };
}
