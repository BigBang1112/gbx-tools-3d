using GbxTools3D.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDomainServices();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);
builder.Services.AddWebServices();
builder.Services.AddCacheServices();

var app = builder.Build();

app.MigrateDatabase();

app.UseMiddleware();

app.Run();
