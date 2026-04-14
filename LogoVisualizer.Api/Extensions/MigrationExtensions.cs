using LogoVisualizer.Data;
using Microsoft.EntityFrameworkCore;

namespace LogoVisualizer.Api.Extensions;

public static class MigrationExtensions
{
    /// <summary>Applies any pending EF Core migrations on startup. Use in Development only.</summary>
    public static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}
