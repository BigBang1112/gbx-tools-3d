using GbxTools3D.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDomainServices();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);
builder.Services.AddWebServices();

var app = builder.Build();

app.MigrateDatabase();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSecurityMiddleware();
app.UseAuthMiddleware();
app.UseEndpointMiddleware();

app.Run();
