using BowlingTrackerSupreme.Blazor.Components;
using BowlingTrackerSupreme.Infrastructure;
using BowlingTrackerSupreme.Migrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();


builder.Services.AddBowlingTrackerSupremeInfrastructure(builder.Configuration, contextOptionsBuilder =>
{
    contextOptionsBuilder.AllowMigrationManagement();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BowlingTrackerSupreme.Blazor.Client._Imports).Assembly);

app.Run();