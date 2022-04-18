using Dynaframe3.Client;
using Dynaframe3.Client.Services;
using Dynaframe3.Shared.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddScoped(sp =>
{
    var navManager = sp.GetRequiredService<NavigationManager>();
    return new HubConnectionBuilder()
                .WithAutomaticReconnect(new RetryPolicy())
                .WithUrl(navManager.ToAbsoluteUri("/Hub"))
                .Build();
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<DevicesService>();
builder.Services.AddScoped<StateContainer>();
builder.Services.AddScoped<DeviceSignalRService>();


await builder.Build().RunAsync();


