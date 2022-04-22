using Dynaframe3.Server;
using Dynaframe3.Server.Data;
using Dynaframe3.Server.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();

builder.Services.AddHealthChecks();

builder.Host.UseSerilog((ctx, log) =>
{
    log
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .WriteTo.Console()
        .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddHttpClient("")
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(250)))
    ;

builder.Services.AddSignalR();

builder.Services.AddRazorPages();

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
});

builder.Services.AddScoped<CommandProcessor>();

var dbDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Data";
if (!Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddDbContext<ServerDbContext>(x =>
{
    x.UseSqlite($"Data Source={dbDirectory}/dynaframeserver.db");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapHealthChecks("/Heartbeat");
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToPage("/Home");

app.MapHub<DynaframeHub>("/Hub");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();