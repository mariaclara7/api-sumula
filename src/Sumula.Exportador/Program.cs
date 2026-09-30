using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.TestHost;
using Sumula.Api;
using Sumula.Exportador;

// Gera a "API pré-calculada": sobe a api-sumula em memória, chama cada endereço que o site usa e grava a
// resposta como arquivo .json. O resultado é publicado na Cloudflare como arquivos estáticos (veja o README).

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.WebHost.UseTestServer();
AplicacaoApi.ConfigurarServicos(builder);

await using var app = builder.Build();
AplicacaoApi.ConfigurarPipeline(app);
await app.StartAsync();

var http = app.GetTestClient();
var saida = Path.GetFullPath(builder.Configuration["Exportacao:Saida"] ?? "saida");
var competicoes = builder.Configuration.GetSection("Coleta:Competicoes").Get<string[]>() is { Length: > 0 } lista
    ? lista
    : ["BSA"];
var temporada = builder.Configuration.GetValue<int?>("Coleta:Temporada") ?? DateTime.UtcNow.Year;

if (Directory.Exists(saida))
    Directory.Delete(saida, recursive: true);
Directory.CreateDirectory(saida);

var total = 0;
foreach (var competicao in competicoes.Select(c => c.ToUpperInvariant()).Distinct())
{
    var times = await http.GetFromJsonAsync<List<TimeResumido>>($"/api/{competicao}/{temporada}/times") ?? [];
    if (times.Count == 0)
    {
        Console.Error.WriteLine($"{competicao} {temporada}: nenhum time no banco. Rode o coletor antes.");
        return 1;
    }

    foreach (var rota in RotasEstaticas.Listar(competicao, temporada, times.Select(t => t.Id).ToList()))
    {
        using var resposta = await http.GetAsync(rota.Url);
        if (!resposta.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"{rota.Url} respondeu {(int)resposta.StatusCode}.");
            return 1;
        }

        var arquivo = Path.Combine(saida, rota.Arquivo);
        Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
        await File.WriteAllBytesAsync(arquivo, await resposta.Content.ReadAsByteArrayAsync());
        total++;
    }

    Console.WriteLine($"{competicao} {temporada}: {times.Count} times.");
}

// Cabeçalhos dos arquivos na Cloudflare: qualquer site pode ler (os dados são públicos) e o navegador
// guarda por 5 minutos, já que a coleta roda de 3 em 3 horas.
await File.WriteAllTextAsync(Path.Combine(saida, "_headers"), """
    /api/*
      Access-Control-Allow-Origin: *
      Cache-Control: public, max-age=300

    """);

Console.WriteLine($"{total} arquivos gerados em {saida}.");
return 0;

internal sealed record TimeResumido(int Id);
