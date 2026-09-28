using System.Text.Json;
using System.Text.Json.Serialization;
using Sumula.Api;
using Sumula.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SumulaDbContext>(o => o.UsarPostgres(ConexaoPostgres.Obter(builder.Configuration)));
builder.Services.AddScoped<RepositorioSumula>();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// Os dados só mudam quando o coletor roda, então as respostas podem ficar alguns minutos em cache.
builder.Services.AddOutputCache(o => o.AddBasePolicy(p => p.Expire(TimeSpan.FromMinutes(5))));

var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origens).AllowAnyHeader().WithMethods("GET")));

var app = builder.Build();

app.UseCors();
app.UseOutputCache();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapEndpointsSumula();

app.Run();
