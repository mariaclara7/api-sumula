using Sumula.Api;

var builder = WebApplication.CreateBuilder(args);
AplicacaoApi.ConfigurarServicos(builder);

var app = builder.Build();
AplicacaoApi.ConfigurarPipeline(app);

app.Run();
