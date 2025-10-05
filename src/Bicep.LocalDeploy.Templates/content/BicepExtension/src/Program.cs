using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MyExtension.Models;
using MyExtension.Handlers.SampleHandler;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
builder.Services
    .AddBicepExtension(
        name: "MyExtension",
        version: "0.0.1",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
        .WithResourceHandler<SampleResourceHandler>();

var app = builder.Build();
app.MapBicepExtension();
await app.RunAsync();
