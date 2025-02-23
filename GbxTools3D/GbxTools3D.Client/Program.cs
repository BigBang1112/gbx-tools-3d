using GBX.NET;
using GBX.NET.LZO;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

Gbx.LZO = new MiniLZO();

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.Configuration["FrontendUrl"] ?? "https://localhost:7130")
    });
builder.Services.AddSingleton<GbxService>();

await builder.Build().RunAsync();
