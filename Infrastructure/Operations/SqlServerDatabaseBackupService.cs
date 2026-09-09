using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentalApp.Application.Interfaces;
using RentalApp.Infrastructure.Configuration;

namespace RentalApp.Infrastructure.Operations;

public sealed class SqlServerDatabaseBackupService(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IOptions<ApplicationDataOptions> appDataOptions,
    ILogger<SqlServerDatabaseBackupService> logger) : IDatabaseBackupService
{
    private const string RestoreConfirmationPhrase = "RESTORE";
    private const string BackupPrefix = "rental-backup";
    private const string PreRestorePrefix = "rental-prerestore";
    private const string BackupExtension = ".bak";

    public Task<IReadOnlyList<DatabaseBackupFileInfo>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backupsDirectory = ResolveBackupsDirectory();

        if (!Directory.Exists(backupsDirectory))
        {
            return Task.FromResult<IReadOnlyList<DatabaseBackupFileInfo>>([]);
        }

        var items = Directory.GetFiles(backupsDirectory, $"{BackupPrefix}-*{BackupExtension}", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(x => x.CreationTimeUtc)
            .Select(x => new DatabaseBackupFileInfo
            {
                FileName = x.Name,
                SizeBytes = x.Length,
                CreatedAtUtc = x.CreationTimeUtc
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<DatabaseBackupFileInfo>>(items);
    }

    public async Task<DatabaseBackupResult> CreateBackupAsync(string? reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backupsDirectory = ResolveBackupsDirectory();
        Directory.CreateDirectory(backupsDirectory);

        var backupFileName = BuildBackupFileName(BackupPrefix);
        var backupPath = Path.Combine(backupsDirectory, backupFileName);

        try
        {
            await BackupDatabaseAsync(backupPath, cancellationToken);
            logger.LogInformation("SQL Server backup created: {BackupFileName}. Reason: {Reason}", backupFileName, reason ?? "manual");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SQL Server database backup failed.");
            throw;
        }

        ApplyRetentionPolicy(backupsDirectory, backupPath);

        var info = new FileInfo(backupPath);
        return new DatabaseBackupResult
        {
            FileName = backupFileName,
            CreatedAtUtc = info.CreationTimeUtc,
            SizeBytes = info.Length
        };
    }

    public async Task<DatabaseRestoreResult> RestoreAsync(string backupFileName, string confirmationPhrase, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(confirmationPhrase?.Trim(), RestoreConfirmationPhrase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid restore confirmation phrase.");
        }

        var safeFileName = ValidateBackupFileName(backupFileName);
        var backupsDirectory = ResolveBackupsDirectory();
        var selectedBackupPath = Path.Combine(backupsDirectory, safeFileName);

        if (!File.Exists(selectedBackupPath))
        {
            throw new FileNotFoundException("Selected backup file was not found.", safeFileName);
        }

        var safetyBackupName = BuildBackupFileName(PreRestorePrefix);
        var safetyBackupPath = Path.Combine(backupsDirectory, safetyBackupName);

        try
        {
            await BackupDatabaseAsync(safetyBackupPath, cancellationToken);
            await RestoreDatabaseAsync(selectedBackupPath, cancellationToken);
            logger.LogWarning("SQL Server restore completed from {SourceBackupFile}. Safety copy: {SafetyBackupFile}", safeFileName, safetyBackupName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SQL Server database restore failed for backup {BackupFileName}.", safeFileName);
            throw;
        }

        return new DatabaseRestoreResult
        {
            RestoredFromFileName = safeFileName,
            SafetyBackupFileName = safetyBackupName,
            RestoredAtUtc = DateTime.UtcNow
        };
    }

    private async Task BackupDatabaseAsync(string backupPath, CancellationToken cancellationToken)
    {
        var databaseName = ResolveDatabaseName();
        var commandText = $"BACKUP DATABASE {EscapeIdentifier(databaseName)} TO DISK = @backupPath WITH COPY_ONLY, INIT, CHECKSUM";

        await ExecuteAgainstMasterAsync(commandText, backupPath, cancellationToken);
    }

    private async Task RestoreDatabaseAsync(string backupPath, CancellationToken cancellationToken)
    {
        var databaseName = ResolveDatabaseName();

        SqlConnection.ClearAllPools();

        try
        {
            await ExecuteAgainstMasterAsync(
                $"ALTER DATABASE {EscapeIdentifier(databaseName)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
                null,
                cancellationToken);

            await ExecuteAgainstMasterAsync(
                $"RESTORE DATABASE {EscapeIdentifier(databaseName)} FROM DISK = @backupPath WITH REPLACE, RECOVERY",
                backupPath,
                cancellationToken);
        }
        finally
        {
            try
            {
                await ExecuteAgainstMasterAsync(
                    $"ALTER DATABASE {EscapeIdentifier(databaseName)} SET MULTI_USER",
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to return database {DatabaseName} to MULTI_USER after restore attempt.", databaseName);
            }

            SqlConnection.ClearAllPools();
        }
    }

    private async Task ExecuteAgainstMasterAsync(string commandText, string? backupPath, CancellationToken cancellationToken)
    {
        var adminConnectionString = ResolveMasterConnectionString();

        await using var connection = new SqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandTimeout = 300;

        if (!string.IsNullOrWhiteSpace(backupPath))
        {
            command.Parameters.Add(new SqlParameter("@backupPath", System.Data.SqlDbType.NVarChar, 4000)
            {
                Value = backupPath
            });
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string ResolveDatabaseName()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        return ApplicationDataPathHelper.GetSqlServerDatabaseName(connectionString);
    }

    private string ResolveMasterConnectionString()
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        return ApplicationDataPathHelper.BuildSqlServerConnectionString(connectionString, "master");
    }

    private string ResolveBackupsDirectory()
    {
        var backupsPath = appDataOptions.Value.BackupsPath;
        if (string.IsNullOrWhiteSpace(backupsPath))
        {
            throw new InvalidOperationException("ApplicationData:BackupsPath is required for database backup operations.");
        }

        var resolved = ApplicationDataPathHelper.ResolvePath(backupsPath, environment.ContentRootPath);

        if (!string.IsNullOrWhiteSpace(environment.WebRootPath) && ApplicationDataPathHelper.IsPathInsideDirectory(resolved, environment.WebRootPath))
        {
            throw new InvalidOperationException("Backups directory must be outside wwwroot.");
        }

        return resolved;
    }

    private void ApplyRetentionPolicy(string backupsDirectory, string latestBackupPath)
    {
        var retentionDays = appDataOptions.Value.BackupRetentionDays;
        if (retentionDays <= 0)
        {
            return;
        }

        var latestBackupFullPath = Path.GetFullPath(latestBackupPath);
        var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);

        var oldFiles = Directory.GetFiles(backupsDirectory, $"{BackupPrefix}-*{BackupExtension}", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFullPath(path), latestBackupFullPath, StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .Where(file => file.CreationTimeUtc < cutoffUtc)
            .ToList();

        foreach (var file in oldFiles)
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete expired backup file {BackupFileName}.", file.Name);
            }
        }
    }

    private static string BuildBackupFileName(string prefix)
    {
        return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{BackupExtension}";
    }

    private static string ValidateBackupFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Backup file name is required.");
        }

        var trimmed = fileName.Trim();
        var expected = Path.GetFileName(trimmed);
        if (!string.Equals(trimmed, expected, StringComparison.Ordinal) || !trimmed.EndsWith(BackupExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid backup file name.");
        }

        return expected;
    }

    private static string EscapeIdentifier(string identifier)
    {
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
