using MLN131.Api.Data;

namespace MLN131.Api.HostedServices;

public sealed class DatabaseSeederHostedService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseSeederHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await SeedData.EnsureSeededAsync(serviceProvider, stoppingToken);
            logger.LogInformation("Database seeding completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database seeding failed.");
        }
    }
}

