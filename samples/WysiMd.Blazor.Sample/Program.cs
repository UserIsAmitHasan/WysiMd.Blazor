using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WysiMd.Blazor;
using WysiMd.Blazor.Sample;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddWysiMdBlazor();

await builder.Build().RunAsync();
