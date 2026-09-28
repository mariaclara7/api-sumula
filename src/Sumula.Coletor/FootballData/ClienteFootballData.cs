using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Sumula.Coletor.FootballData;

public class ClienteFootballData(HttpClient http, ILogger<ClienteFootballData> logger)
{
    public const string UrlBase = "https://api.football-data.org/v4/";

    public Task<RespostaTimes> ObterTimesAsync(string competicao, int temporada, CancellationToken ct) =>
        ObterAsync<RespostaTimes>($"competitions/{competicao}/teams?season={temporada}", ct);

    public Task<RespostaPartidas> ObterPartidasAsync(string competicao, int temporada, CancellationToken ct) =>
        ObterAsync<RespostaPartidas>($"competitions/{competicao}/matches?season={temporada}", ct);

    public Task<RespostaArtilharia> ObterArtilhariaAsync(string competicao, int temporada, CancellationToken ct) =>
        ObterAsync<RespostaArtilharia>($"competitions/{competicao}/scorers?season={temporada}&limit=100", ct);

    private async Task<T> ObterAsync<T>(string caminho, CancellationToken ct)
    {
        // O plano gratuito permite 10 requisições por minuto. Se estourar, espera e tenta de novo.
        for (var tentativa = 1; ; tentativa++)
        {
            using var resposta = await http.GetAsync(caminho, ct);

            if (resposta.StatusCode == HttpStatusCode.TooManyRequests && tentativa < 3)
            {
                var espera = resposta.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                logger.LogWarning("Limite de requisições atingido. Aguardando {Segundos}s.", espera.TotalSeconds);
                await Task.Delay(espera, ct);
                continue;
            }

            if (!resposta.IsSuccessStatusCode)
            {
                var corpo = await resposta.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"football-data.org respondeu {(int)resposta.StatusCode} para {caminho}: {corpo}");
            }

            return await resposta.Content.ReadFromJsonAsync<T>(ct)
                ?? throw new InvalidOperationException($"Resposta vazia para {caminho}.");
        }
    }
}
