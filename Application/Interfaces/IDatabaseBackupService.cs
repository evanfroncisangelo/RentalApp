namespace RentalApp.Application.Interfaces;

public interface IDatabaseBackupService
{
    Task<DatabaseBackupResult> CreateBackupAsync(string? reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatabaseBackupFileInfo>> GetBackupsAsync(CancellationToken cancellationToken = default);
    Task<DatabaseRestoreResult> RestoreAsync(string backupFileName, string confirmationPhrase, CancellationToken cancellationToken = default);
}

public sealed class DatabaseBackupFileInfo
{
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class DatabaseBackupResult
{
    public string FileName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public long SizeBytes { get; init; }
}

public sealed class DatabaseRestoreResult
{
    public string RestoredFromFileName { get; init; } = string.Empty;
    public string SafetyBackupFileName { get; init; } = string.Empty;
    public DateTime RestoredAtUtc { get; init; }
}
