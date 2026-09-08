using Altivy.Notes.Worker.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddVaultProvisioningWorker(builder.Configuration, builder.Environment);

await builder.Build().RunAsync();
