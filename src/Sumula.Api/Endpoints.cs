using Sumula.Core.Estatisticas;
using Sumula.Core.Modelos;
using Sumula.Data;

namespace Sumula.Api;

public static class Endpoints
{
    public static void MapEndpointsSumula(this IEndpointRouteBuilder app)
    {
        // Ex.: /api/BSA/2026/classificacao. A competição usa os códigos do football-data.org.
        var grupo = app.MapGroup("/api/{competicao}/{temporada:int}").CacheOutput();

        grupo.MapGet("/times", async (string competicao, int temporada, RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            return Results.Ok(dados.Times);
        });

        grupo.MapGet("/classificacao", async (
            string competicao, int temporada, string? recorte, string? mando, string? tempo,
            RepositorioSumula repo, CancellationToken ct) =>
        {
            if (!TentarLer(recorte, Recorte.Geral, out var recorteEscolhido) ||
                !TentarLer(mando, Mando.Todos, out var mandoEscolhido) ||
                !TentarLer(tempo, Tempo.JogoTodo, out var tempoEscolhido))
                return Results.BadRequest(new
                {
                    erro = "Use recorte=geral|primeiroTurno|segundoTurno, mando=todos|casa|fora " +
                           "e tempo=jogoTodo|primeiroTempo|segundoTempo.",
                });

            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            var filtro = Filtro.DoRecorte(recorteEscolhido, dados.Times.Count, mandoEscolhido);

            // "Se o jogo acabasse no intervalo" / "se só valesse o 2º tempo".
            // Jogos sem o placar do intervalo ficam de fora desses dois recortes.
            var partidas = dados.Partidas.Select(p => p.NoTempo(tempoEscolhido)).OfType<Partida>().ToList();

            return Results.Ok(new
            {
                recorte = recorteEscolhido,
                mando = mandoEscolhido,
                tempo = tempoEscolhido,
                atualizadoEm = await repo.ObterUltimaColetaAsync(Codigo(competicao), temporada, ct),
                linhas = CalculadoraClassificacao.Calcular(dados.Times, partidas, filtro),
            });
        });

        grupo.MapGet("/tempos", async (string competicao, int temporada, RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            var gols = await repo.ObterGolsAsync(Codigo(competicao), temporada, ct);
            return Results.Ok(CalculadoraTempos.Calcular(dados.Times, dados.Partidas, gols));
        });

        grupo.MapGet("/partidas", async (
            string competicao, int temporada, int? rodada, int? timeId,
            RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            var partidas = dados.Partidas
                .Where(p => rodada is null || p.Rodada == rodada)
                .Where(p => timeId is null || p.Envolve(timeId.Value));
            return Results.Ok(partidas);
        });

        grupo.MapGet("/times/{timeId:int}", async (
            string competicao, int temporada, int timeId, RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            var resumo = ResumoTime.Montar(timeId, dados.Times, dados.Partidas);
            return resumo is null ? Results.NotFound() : Results.Ok(resumo);
        });

        grupo.MapGet("/confronto", async (
            string competicao, int temporada, int? timeA, int? timeB,
            RepositorioSumula repo, CancellationToken ct) =>
        {
            if (timeA is null || timeB is null || timeA == timeB)
                return Results.BadRequest(new { erro = "Informe dois times diferentes em timeA e timeB." });

            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            if (dados.Times.All(t => t.Id != timeA) || dados.Times.All(t => t.Id != timeB))
                return Results.NotFound();

            return Results.Ok(CalculadoraConfronto.Resumir(timeA.Value, timeB.Value, dados.Partidas));
        });

        grupo.MapGet("/palpites", async (
            string competicao, int temporada, int? rodada, int? timeId,
            RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            var filtradas = dados.Partidas
                .Where(p => rodada is null || p.Rodada == rodada)
                .Where(p => timeId is null || p.Envolve(timeId.Value))
                .Select(p => p.Id)
                .ToHashSet();

            // O modelo usa todos os jogos da temporada; o filtro só escolhe quais palpites devolver.
            var palpites = CalculadoraPalpite.Calcular(dados.Times, dados.Partidas).Where(p => filtradas.Contains(p.PartidaId));
            return Results.Ok(palpites);
        });

        grupo.MapGet("/matematica", async (string competicao, int temporada, RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            return Results.Ok(CalculadoraMatematica.Calcular(dados.Times, dados.Partidas));
        });

        grupo.MapGet("/estatisticas", async (string competicao, int temporada, RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            return Results.Ok(CalculadoraEstatisticas.Calcular(dados.Times, dados.Partidas));
        });

        grupo.MapGet("/evolucao", async (string competicao, int temporada, RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);
            var evolucao = CalculadoraEvolucao.Calcular(dados.Times, dados.Partidas);
            return Results.Ok(evolucao.Select(par => new { timeId = par.Key, rodadas = par.Value }));
        });

        grupo.MapGet("/probabilidades", async (string competicao, int temporada, RepositorioSumula repo, CancellationToken ct) =>
        {
            var dados = await repo.ObterTemporadaAsync(Codigo(competicao), temporada, ct);

            // A semente depende só dos jogos já disputados: enquanto nada muda, o resultado
            // é o mesmo e os percentuais não "tremem" a cada atualização do cache.
            var semente = dados.Partidas.Count(p => p.TemResultado);
            return Results.Ok(SimuladorCampeonato.Simular(dados.Times, dados.Partidas, semente: semente));
        });

        grupo.MapGet("/artilharia", async (string competicao, int temporada, RepositorioSumula repo, CatalogoFotos fotos, CancellationToken ct) =>
            Results.Ok((await repo.ObterArtilhariaAsync(Codigo(competicao), temporada, ct)).Select(fotos.ComFoto)));
    }

    private static string Codigo(string competicao) => competicao.ToUpperInvariant();

    private static bool TentarLer<T>(string? valor, T padrao, out T resultado) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            resultado = padrao;
            return true;
        }

        return Enum.TryParse(valor, ignoreCase: true, out resultado) && Enum.IsDefined(resultado);
    }
}
