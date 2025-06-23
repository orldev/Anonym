using Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddReactiveTransferSignalR(builder.Configuration);
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapReactiveTransferHub();
app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();
