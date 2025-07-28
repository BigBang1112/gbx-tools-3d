using GBX.NET;
using GbxTools3D.Configuration;

Gbx.LZO = new GBX.NET.LZO.Lzo();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDomainServices();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);
builder.Services.AddWebServices(builder.Configuration, builder.Environment);
builder.Services.AddCacheServices();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.MigrateDatabase();
}

app.UseMiddleware();

app.Run();
