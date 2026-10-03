using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sumula.Coletor;
using Sumula.Coletor.FootballData;
using Sumula.Coletor.Fotos;
using Sumula.Data;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

var token = builder.Configuration["FootballData:Token"];
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine(
        "Token do football-data.org não configurado. Use 'dotnet user-secrets set FootballData:Token <token>' " +
        "ou a variável de ambiente FootballData__Token.");
    return 1;
}

builder.Services.Configure<OpcoesColeta>(builder.Configuration.GetSection("Coleta"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<SumulaDbContext>(o => o.UsarPostgres(ConexaoPostgres.Obter(builder.Configuration)));
builder.Services.AddHttpClient<ClienteFootballData>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["FootballData:UrlBase"] ?? ClienteFootballData.UrlBase);
    http.DefaultRequestHeaders.Add("X-Auth-Token", token);

    // Com o plano "Free + Deep Data" as partidas passam a trazer os gols com minuto.
    if (builder.Configuration.GetValue<bool>("FootballData:DadosDetalhados"))
        http.DefaultRequestHeaders.Add("X-Unfold-Goals", "true");
});
builder.Services.AddHttpClient<ClienteWikimedia>(http =>
    // A regra de uso das APIs da Wikimedia pede um User-Agent que identifique quem chama.
    http.DefaultRequestHeaders.UserAgent.ParseAdd(
        "BraSumula/1.0 (https://brasumula.com.br; https://github.com/mariaclara7/api-sumula)"));
builder.Services.AddScoped<Coletor>();
builder.Services.AddScoped<BuscadorFotos>();

using var host = builder.Build();
using var escopo = host.Services.CreateScope();

// "dotnet run --project src/Sumula.Coletor -- fotos": procura as fotos dos artilheiros (não usa o banco).
if (args.Contains("fotos"))
{
    var arquivo = builder.Configuration["Fotos:Arquivo"] ?? Path.Combine("dados", "fotos-jogadores.json");
    await escopo.ServiceProvider.GetRequiredService<BuscadorFotos>().ExecutarAsync(arquivo, CancellationToken.None);
    return 0;
}

await escopo.ServiceProvider.GetRequiredService<Coletor>().ExecutarAsync(CancellationToken.None);
return 0;
