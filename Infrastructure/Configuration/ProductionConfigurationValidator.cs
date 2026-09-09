using Microsoft.Extensions.Options;
using RentalApp.Infrastructure.Security;

namespace RentalApp.Infrastructure.Configuration;

public sealed class ProductionConfigurationValidator(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IOptions<JwtOptions> jwtOptions,
    IOptions<ApplicationDataOptions> appDataOptions) : IHostedService
{
    private static readonly string[] PlaceholderJwtMarkers =
    [
        "replace_with",
        "changeme",
        "your_",
        "development",
        "sample",
        "placeholder"
    ];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsProduction())
        {
            return Task.CompletedTask;
        }

        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("Production configuration is incomplete. Required configuration: ConnectionStrings__DefaultConnection.");
        }

        var sqlServerConnection = ApplicationDataPathHelper.GetSqlServerConnectionStringBuilder(defaultConnection);
        if (string.IsNullOrWhiteSpace(sqlServerConnection.DataSource))
        {
            throw new InvalidOperationException("Production configuration is invalid. DefaultConnection must include a SQL Server data source.");
        }

        if (string.IsNullOrWhiteSpace(sqlServerConnection.InitialCatalog))
        {
            throw new InvalidOperationException("Production configuration is invalid. DefaultConnection must include a SQL Server database name.");
        }

        var appData = appDataOptions.Value;
        EnsureConfiguredPath(appData.RootPath, "ApplicationData__RootPath");
        EnsureConfiguredPath(appData.UploadsPath, "ApplicationData__UploadsPath");
        EnsureConfiguredPath(appData.BackupsPath, "ApplicationData__BackupsPath");

        if (appData.BackupRetentionDays <= 0 || appData.BackupRetentionDays > 3650)
        {
            throw new InvalidOperationException("Production configuration is invalid. ApplicationData__BackupRetentionDays must be between 1 and 3650.");
        }

        ValidateAppDataPath(appData.RootPath, environment, "ApplicationData__RootPath");
        ValidateAppDataPath(appData.UploadsPath, environment, "ApplicationData__UploadsPath");
        ValidateAppDataPath(appData.BackupsPath, environment, "ApplicationData__BackupsPath");

        var jwt = jwtOptions.Value;
        if (string.IsNullOrWhiteSpace(jwt.Issuer))
        {
            throw new InvalidOperationException("Production configuration is incomplete. Required configuration: Jwt__Issuer.");
        }

        if (string.IsNullOrWhiteSpace(jwt.Audience))
        {
            throw new InvalidOperationException("Production configuration is incomplete. Required configuration: Jwt__Audience.");
        }

        if (jwt.ExpiresMinutes <= 0 || jwt.ExpiresMinutes > 1440)
        {
            throw new InvalidOperationException("Production configuration is invalid. Jwt__ExpiresMinutes must be between 1 and 1440.");
        }

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32 || LooksLikePlaceholder(jwt.Key))
        {
            throw new InvalidOperationException("Production configuration is incomplete. Required configuration: strong Jwt__Key.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void EnsureConfiguredPath(string? value, string configName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Production configuration is incomplete. Required configuration: {configName}.");
        }
    }

    private static void ValidateAppDataPath(string? path, IWebHostEnvironment environment, string configName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var resolvedPath = ApplicationDataPathHelper.ResolvePath(path, environment.ContentRootPath);
        if (!string.IsNullOrWhiteSpace(environment.WebRootPath) && ApplicationDataPathHelper.IsPathInsideDirectory(resolvedPath, environment.WebRootPath))
        {
            throw new InvalidOperationException($"Production configuration is invalid. {configName} must be outside wwwroot.");
        }
    }

    private static bool LooksLikePlaceholder(string jwtKey)
    {
        var normalized = jwtKey.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();

        return PlaceholderJwtMarkers.Any(normalized.Contains);
    }

}
