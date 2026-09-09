using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RentalApp.Application.Interfaces;
using RentalApp.Infrastructure.Configuration;
using RentalApp.Infrastructure.Operations;

namespace RentalApp.Tests;

public class SqlServerDatabaseBackupServiceTests
{
    [Fact]
    public async Task GetBackupsAsync_ReturnsManagedBackupFiles()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        var backupPath = Path.Combine(fixture.BackupsPath, "rental-backup-20260101010101001.bak");
        await File.WriteAllTextAsync(backupPath, "sample-backup");

        var service = fixture.CreateService();
        var backups = await service.GetBackupsAsync();

        Assert.Contains(backups, x => x.FileName == Path.GetFileName(backupPath));
    }

    [Fact]
    public async Task RestoreAsync_InvalidConfirmation_Throws()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        var service = fixture.CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreAsync("rental-backup-20260101010101001.bak", "WRONG"));
    }

    [Fact]
    public async Task RestoreAsync_PathTraversalFileName_Throws()
    {
        await using var fixture = await BackupFixture.CreateAsync();
        var service = fixture.CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreAsync("../outside.bak", "RESTORE"));
    }

    private sealed class BackupFixture : IAsyncDisposable
    {
        private readonly string _rootPath;

        private BackupFixture(string rootPath, string backupsPath)
        {
            _rootPath = rootPath;
            BackupsPath = backupsPath;
        }

        public string BackupsPath { get; }

        public static Task<BackupFixture> CreateAsync()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), $"rentalapp-backup-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(rootPath);

            var backupsPath = Path.Combine(rootPath, "Backups");
            Directory.CreateDirectory(backupsPath);

            return Task.FromResult(new BackupFixture(rootPath, backupsPath));
        }

        public IDatabaseBackupService CreateService()
        {
            var configValues = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=RentalAppDb;Integrated Security=true;Encrypt=false;TrustServerCertificate=true",
                ["ApplicationData:BackupsPath"] = BackupsPath,
                ["ApplicationData:BackupRetentionDays"] = "30"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            var environment = new TestWebHostEnvironment
            {
                ContentRootPath = _rootPath,
                WebRootPath = Path.Combine(_rootPath, "wwwroot")
            };

            var options = Options.Create(new ApplicationDataOptions
            {
                BackupsPath = BackupsPath,
                BackupRetentionDays = 30
            });

            return new SqlServerDatabaseBackupService(configuration, environment, options, NullLogger<SqlServerDatabaseBackupService>.Instance);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "RentalApp.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = default!;
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = default!;
    }
}
