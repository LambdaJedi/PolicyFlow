using Microsoft.EntityFrameworkCore;
using PolicyFlow.Components;
using PolicyFlow.Data;
using PolicyFlow.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<PolicyFlowDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PolicyService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Optional: apply migrations and insert sample data on startup.
// Enabled in Development (see appsettings.Development.json) and in docker-compose.
if (app.Configuration.GetValue<bool>("Database:MigrateAndSeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<PolicyFlowDbContext>>();

    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. See https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
