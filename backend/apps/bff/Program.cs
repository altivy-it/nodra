using Altivy.Bff.Configuration;
using Altivy.Bff.DependencyInjection;
using Altivy.Bff.Endpoints;
using Altivy.Bff.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddBffConfiguration()
    .AddBffServices();

var app = builder.Build();

app.UseBff();
app.MapBffEndpoints();

app.Run();

/// <summary>
/// Expõe o ponto de entrada parcial do BFF para integração e testes do host ASP.NET Core.
/// </summary>
public partial class Program;