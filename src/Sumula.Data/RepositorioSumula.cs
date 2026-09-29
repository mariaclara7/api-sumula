using Microsoft.EntityFrameworkCore;
using Sumula.Core.Modelos;

namespace Sumula.Data;

public sealed record DadosTemporada(IReadOnlyList<Time> Times, IReadOnlyList<Partida> Partidas);

public class RepositorioSumula(SumulaDbContext db)
{
    public async Task<DadosTemporada> ObterTemporadaAsync(string competicao, int temporada, CancellationToken ct = default)
    {
        var times = await db.Participacoes
            .AsNoTracking()
            .Where(p => p.Competicao == competicao && p.Temporada == temporada)
            .Select(p => p.Time)
            .OrderBy(t => t.Nome)
            .ToListAsync(ct);

        var partidas = await db.Partidas
            .AsNoTracking()
            .Where(p => p.Competicao == competicao && p.Temporada == temporada)
            .OrderBy(p => p.Data)
            .ToListAsync(ct);

        return new DadosTemporada(
            times.Select(t => t.ParaModelo()).ToList(),
            partidas.Select(p => p.ParaModelo()).ToList());
    }

    public async Task<IReadOnlyList<Artilheiro>> ObterArtilhariaAsync(string competicao, int temporada, CancellationToken ct = default)
    {
        var artilheiros = await db.Artilheiros
            .AsNoTracking()
            .Where(a => a.Competicao == competicao && a.Temporada == temporada)
            .OrderByDescending(a => a.Gols)
            .ThenByDescending(a => a.Assistencias)
            .ThenBy(a => a.Nome)
            .ToListAsync(ct);

        return artilheiros.Select(a => a.ParaModelo()).ToList();
    }

    /// <summary>Gols com minuto. Fica vazio enquanto a coleta não usa os dados detalhados.</summary>
    public async Task<IReadOnlyList<Gol>> ObterGolsAsync(string competicao, int temporada, CancellationToken ct = default)
    {
        var gols = await db.Gols
            .AsNoTracking()
            .Where(g => db.Partidas.Any(p => p.Id == g.PartidaId && p.Competicao == competicao && p.Temporada == temporada))
            .ToListAsync(ct);

        return gols.Select(g => g.ParaModelo()).ToList();
    }

    public Task<DateTimeOffset?> ObterUltimaColetaAsync(string competicao, int temporada, CancellationToken ct = default) =>
        db.Coletas
            .AsNoTracking()
            .Where(c => c.Competicao == competicao && c.Temporada == temporada)
            .MaxAsync(c => (DateTimeOffset?)c.ExecutadaEm, ct);
}
