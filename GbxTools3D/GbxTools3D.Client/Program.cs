using GBX.NET;
using GBX.NET.LZO;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

Gbx.LZO = new MiniLZO();

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });
builder.Services.AddSingleton<GbxService>();
builder.Services.AddScoped<ICollectionClientService, CollectionClientService>();
builder.Services.AddScoped<IBlockClientService, BlockClientService>();
builder.Services.AddScoped<IDecorationClientService, DecorationClientService>();

await builder.Build().RunAsync();
