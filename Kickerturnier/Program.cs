using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using Kickerturnier;
using Kickerturnier.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add Fluent UI services
builder.Services.AddFluentUIComponents();

// Register tournament services as singleton to maintain state across navigation
builder.Services.AddSingleton<TournamentService>();
builder.Services.AddSingleton<LocalStorageService>();

await builder.Build().RunAsync();
