using Santy.Web.Components;
using Santy.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure file logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile("santy-web.log", append: true);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Santy services
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<AppStateService>();
builder.Services.AddScoped<SantyOperationsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
