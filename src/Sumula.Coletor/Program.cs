using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sumula.Coletor;
using Sumula.Coletor.FootballData;
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
});
builder.Services.AddScoped<Coletor>();

using var host = builder.Build();
using var escopo = host.Services.CreateScope();
await escopo.ServiceProvider.GetRequiredService<Coletor>().ExecutarAsync(CancellationToken.None);
return 0;
