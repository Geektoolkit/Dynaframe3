using Dynaframe3.Server;
using Dynaframe3.Server.Data;
using Dynaframe3.Server.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();

builder.Host.UseSerilog((ctx, log) =>
{
    log
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .WriteTo.Console()
        .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddHttpClient();

builder.Services.AddSignalR();

builder.Services.AddRazorPages();

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
});

builder.Services.AddScoped<CommandProcessor>();

builder.Services.AddDbContext<ServerDbContext>(x =>
{
    x.UseSqlite("Data Source=./Data/dynaframeserver.db");
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

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToPage("/Home");

app.MapHub<DynaframeHub>("/Hub");

if (!File.Exists("./Data/dynaframeserver.db"))
{
    File.Copy("./dynaframeserver.db", "./Data/dynaframeserver.db");
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();