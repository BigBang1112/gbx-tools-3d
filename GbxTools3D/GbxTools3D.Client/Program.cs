using GBX.NET;
using GBX.NET.LZO;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

Gbx.LZO = new MiniLZO();

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.Configuration["FrontendUrl"] ?? "https://localhost:7130")
    });

await builder.Build().RunAsync();
