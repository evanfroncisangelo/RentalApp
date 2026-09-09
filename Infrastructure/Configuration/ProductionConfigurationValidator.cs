using Microsoft.Extensions.Options;
using RentalApp.Infrastructure.Security;

namespace RentalApp.Infrastructure.Configuration;

public sealed class ProductionConfigurationValidator(
    IConfiguration configuration,
    IHostEnvironment environment,
    IOptions<JwtOptions> jwtOptions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsProduction())
        {
            return Task.CompletedTask;
        }

        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("Production configuration is incomplete. Required configuration: Database connection.");
        }

        var jwt = jwtOptions.Value;
        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            throw new InvalidOperationException("Production configuration is incomplete. Required configuration: JWT signing key.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
