using System.Text.Json;
using System.Text.Json.Serialization;
using Sumula.Data;

namespace Sumula.Api;

/// <summary>
/// Configuração da API em um lugar só, usada pelo Program.cs e pelo Sumula.Exportador,
/// que sobe a mesma API em memória para gerar os arquivos estáticos.
/// </summary>
public static class AplicacaoApi
{
    public static void ConfigurarServicos(WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<SumulaDbContext>(o => o.UsarPostgres(ConexaoPostgres.Obter(builder.Configuration)));
        builder.Services.AddScoped<RepositorioSumula>();
        builder.Services.AddSingleton<CatalogoFotos>();

        builder.Services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

        // Os dados só mudam quando o coletor roda, então as respostas podem ficar alguns minutos em cache.
        builder.Services.AddOutputCache(o => o.AddBasePolicy(p => p.Expire(TimeSpan.FromMinutes(5))));

        var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origens).AllowAnyHeader().WithMethods("GET")));
    }

    public static void ConfigurarPipeline(WebApplication app)
    {
        app.UseCors();
        app.UseOutputCache();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapEndpointsSumula();
    }
}
