using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.Core.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddLocalStorage();
builder.Services.AddAuthorization();
builder.Services.AddAuthHttpClient(builder.HostEnvironment.BaseAddress);
await builder.Services.AddHubConnectionAsync(builder.HostEnvironment.BaseAddress);


await builder.Build().RunAsync();