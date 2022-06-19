using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Olwe.Data;

namespace Olwe.Services;

[PrimaryConstructor]
public partial class DbMigrationService : BackgroundService
{
    private readonly ILogger<DbMigrationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        await using var db = scope.ServiceProvider.GetRequiredService<OlweContext>();
        _logger.LogInformation("Checking for database migrations...");
        var migrations = await db.Database.GetPendingMigrationsAsync(stoppingToken);
        if (migrations.Any())
        {
            _logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync(stoppingToken);
            _logger.LogInformation("Database migrations applied");
        }
        else
        {
            _logger.LogInformation("No database migrations to apply");
        }
    }
}