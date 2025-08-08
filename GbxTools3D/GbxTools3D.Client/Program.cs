using GBX.NET;
using GBX.NET.LZO;
using GBX.NET.ZLib;
using GbxTools3D.Client.Configuration;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

Gbx.LZO = new MiniLZO();
Gbx.ZLib = new ZLib();

builder.Services.AddDomainServices();
builder.Services.AddInfrastructureServices(builder.HostEnvironment);
builder.Services.AddSingleton<StateService>();

await builder.Build().RunAsync();
