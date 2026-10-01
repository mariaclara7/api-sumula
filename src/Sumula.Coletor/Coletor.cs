using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sumula.Coletor.FootballData;
using Sumula.Data;
using Sumula.Data.Entidades;

namespace Sumula.Coletor;

public class Coletor(
    ClienteFootballData cliente,
    SumulaDbContext db,
    IOptions<OpcoesColeta> opcoes,
    TimeProvider relogio,
    ILogger<Coletor> logger)
{
    public async Task ExecutarAsync(CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);

        var temporada = opcoes.Value.Temporada ?? relogio.GetUtcNow().Year;
        var competicoes = opcoes.Value.Competicoes.Select(c => c.ToUpperInvariant()).Distinct().ToList();
        if (competicoes.Count == 0)
            competicoes.Add("BSA");

        foreach (var competicao in competicoes)
        {
            await ColetarAsync(competicao, temporada, ct);
            db.ChangeTracker.Clear();
        }
    }

    private async Task ColetarAsync(string competicao, int temporada, CancellationToken ct)
    {
        logger.LogInformation("Coletando {Competicao} {Temporada}...", competicao, temporada);

        var times = await cliente.ObterTimesAsync(competicao, temporada, ct);
        var partidas = await cliente.ObterPartidasAsync(competicao, temporada, ct);
        var artilharia = await cliente.ObterArtilhariaAsync(competicao, temporada, ct);

        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        await SalvarTimesAsync(competicao, temporada, times.Teams, ct);
        var quantidadePartidas = await SalvarPartidasAsync(competicao, temporada, partidas.Matches, ct);
        await SalvarArtilhariaAsync(competicao, temporada, artilharia.Scorers, ct);

        db.Coletas.Add(new ColetaEntidade
        {
            Competicao = competicao,
            Temporada = temporada,
            ExecutadaEm = relogio.GetUtcNow(),
            PartidasAtualizadas = quantidadePartidas,
        });

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        logger.LogInformation(
            "{Competicao} {Temporada}: {Times} times, {Partidas} partidas, {Artilheiros} artilheiros.",
            competicao, temporada, times.Teams.Count, quantidadePartidas, artilharia.Scorers.Count);
    }

    private async Task SalvarTimesAsync(string competicao, int temporada, List<TimeFd> times, CancellationToken ct)
    {
        var validos = times.Where(t => t.Id is not null).ToList();
        var ids = validos.Select(t => t.Id!.Value).ToList();

        var existentes = await db.Times.Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);
        var participantes = (await db.Participacoes
            .Where(p => p.Competicao == competicao && p.Temporada == temporada)
            .Select(p => p.TimeId)
            .ToListAsync(ct)).ToHashSet();

        foreach (var time in validos)
        {
            var id = time.Id!.Value;
            if (!existentes.TryGetValue(id, out var entidade))
            {
                entidade = new TimeEntidade { Id = id, Nome = "", NomeCurto = "", Sigla = "" };
                db.Times.Add(entidade);
            }

            var nomes = Conversao.Nomes(time);
            // Mostra no log como cada clube vem da fonte, para facilitar ajustar nomes em Conversao.Nomes.
            logger.LogInformation("Time {Id}: name \"{Name}\", shortName \"{ShortName}\", tla \"{Tla}\" -> \"{NomeCurto}\" ({Sigla})",
                id, time.Name, time.ShortName, time.Tla, nomes.NomeCurto, nomes.Sigla);
            entidade.Nome = nomes.Nome;
            entidade.NomeCurto = nomes.NomeCurto;
            entidade.Sigla = nomes.Sigla;
            entidade.Escudo = time.Crest;

            if (!participantes.Contains(id))
                db.Participacoes.Add(new ParticipacaoEntidade { Competicao = competicao, Temporada = temporada, TimeId = id });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<int> SalvarPartidasAsync(string competicao, int temporada, List<PartidaFd> partidas, CancellationToken ct)
    {
        // Em mata-mata o adversário pode ainda não estar definido: esses jogos ficam para a próxima coleta.
        var validas = partidas.Where(p => p.HomeTeam.Id is not null && p.AwayTeam.Id is not null).ToList();
        var ids = validas.Select(p => p.Id).ToList();
        var existentes = await db.Partidas.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        foreach (var partida in validas)
        {
            if (!existentes.TryGetValue(partida.Id, out var entidade))
            {
                entidade = new PartidaEntidade { Id = partida.Id, Competicao = competicao, Temporada = temporada };
                db.Partidas.Add(entidade);
            }

            entidade.Rodada = partida.Matchday ?? 0;
            entidade.Data = partida.UtcDate;
            entidade.Status = Conversao.Status(partida.Status);
            entidade.MandanteId = partida.HomeTeam.Id!.Value;
            entidade.VisitanteId = partida.AwayTeam.Id!.Value;
            entidade.GolsMandante = partida.Score.FullTime.Home;
            entidade.GolsVisitante = partida.Score.FullTime.Away;
            entidade.GolsMandanteIntervalo = partida.Score.HalfTime?.Home;
            entidade.GolsVisitanteIntervalo = partida.Score.HalfTime?.Away;
        }

        await SalvarGolsAsync(validas, ct);
        return validas.Count;
    }

    /// <summary>Só roda com os dados detalhados; no plano gratuito as partidas vêm sem a lista de gols.</summary>
    private async Task SalvarGolsAsync(List<PartidaFd> partidas, CancellationToken ct)
    {
        var comGols = partidas.Where(p => p.Goals is not null).ToList();
        if (comGols.Count == 0)
            return;

        var ids = comGols.Select(p => p.Id).ToList();
        await db.Gols.Where(g => ids.Contains(g.PartidaId)).ExecuteDeleteAsync(ct);

        foreach (var partida in comGols)
        {
            foreach (var gol in Conversao.Gols(partida))
            {
                db.Gols.Add(new GolEntidade
                {
                    PartidaId = partida.Id,
                    Minuto = gol.Minuto,
                    Acrescimo = gol.Acrescimo,
                    TimeId = gol.TimeId,
                    Tipo = gol.Tipo,
                    Autor = gol.Autor,
                });
            }
        }
    }

    private async Task SalvarArtilhariaAsync(string competicao, int temporada, List<ArtilheiroFd> artilheiros, CancellationToken ct)
    {
        // A lista é sempre o ranking completo, então substituímos a anterior.
        await db.Artilheiros
            .Where(a => a.Competicao == competicao && a.Temporada == temporada)
            .ExecuteDeleteAsync(ct);

        foreach (var artilheiro in artilheiros.Where(a => a.Team.Id is not null).DistinctBy(a => a.Player.Id))
        {
            db.Artilheiros.Add(new ArtilheiroEntidade
            {
                Competicao = competicao,
                Temporada = temporada,
                JogadorId = artilheiro.Player.Id,
                Nome = artilheiro.Player.Name,
                TimeId = artilheiro.Team.Id!.Value,
                Jogos = artilheiro.PlayedMatches,
                Gols = artilheiro.Goals ?? 0,
                Assistencias = artilheiro.Assists,
                Penaltis = artilheiro.Penalties,
            });
        }
    }
}
