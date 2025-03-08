using GBX.NET;
using GBX.NET.LZO;
using GBX.NET.ZLib;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

Gbx.LZO = new MiniLZO();
Gbx.ZLib = new ZLib();

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });
builder.Services.AddSingleton<GbxService>();
builder.Services.AddScoped<ICollectionClientService, CollectionClientService>();
builder.Services.AddScoped<IBlockClientService, BlockClientService>();
builder.Services.AddScoped<IDecorationClientService, DecorationClientService>();
builder.Services.AddScoped<IVehicleClientService, VehicleClientService>();

await builder.Build().RunAsync();
