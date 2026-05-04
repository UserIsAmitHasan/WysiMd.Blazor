using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using WysiMd.Blazor;
using WysiMd.Blazor.MudBlazor.Sample;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddMudServices();
builder.Services.AddWysiMdBlazor();

await builder.Build().RunAsync();
