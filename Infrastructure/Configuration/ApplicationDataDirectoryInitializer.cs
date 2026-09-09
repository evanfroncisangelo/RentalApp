using Microsoft.Extensions.Options;

namespace RentalApp.Infrastructure.Configuration;

public sealed class ApplicationDataDirectoryInitializer(
    IOptions<ApplicationDataOptions> options,
    IWebHostEnvironment environment,
    ILogger<ApplicationDataDirectoryInitializer> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var appData = options.Value;

        var directories = new List<string>();

        if (!string.IsNullOrWhiteSpace(appData.RootPath))
        {
            directories.Add(ApplicationDataPathHelper.ResolvePath(appData.RootPath, environment.ContentRootPath));
        }

        if (!string.IsNullOrWhiteSpace(appData.UploadsPath))
        {
            directories.Add(ApplicationDataPathHelper.ResolvePath(appData.UploadsPath, environment.ContentRootPath));
        }

        if (!string.IsNullOrWhiteSpace(appData.BackupsPath))
        {
            directories.Add(ApplicationDataPathHelper.ResolvePath(appData.BackupsPath, environment.ContentRootPath));
        }

        var uniqueDirectories = directories
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var directory in uniqueDirectories)
        {
            if (!string.IsNullOrWhiteSpace(environment.WebRootPath) && ApplicationDataPathHelper.IsPathInsideDirectory(directory, environment.WebRootPath))
            {
                throw new InvalidOperationException("Application data directory must not be inside wwwroot.");
            }

            Directory.CreateDirectory(directory);

            if (environment.IsProduction())
            {
                var probeFile = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}.tmp");
                try
                {
                    File.WriteAllText(probeFile, "ok");
                    File.Delete(probeFile);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    logger.LogError(ex, "Application data directory is not writable.");
                    throw new InvalidOperationException("Production configuration is incomplete. Required configuration: Writable application data directory.");
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
