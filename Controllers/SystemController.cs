using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[Authorize]
[Route("api/system")]
public class SystemController(IDatabaseBackupService databaseBackupService) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
        return Ok(new
        {
            app = "RentalApp",
            version,
            utcNow = DateTime.UtcNow
        });
    }

    [HttpGet("backups")]
    public async Task<IActionResult> GetBackups(CancellationToken cancellationToken)
    {
        var backups = await databaseBackupService.GetBackupsAsync(cancellationToken);
        return Ok(backups);
    }

    [HttpPost("backups/create")]
    public async Task<IActionResult> CreateBackup([FromBody] CreateBackupRequest request, CancellationToken cancellationToken)
    {
        var result = await databaseBackupService.CreateBackupAsync(request.Reason, cancellationToken);
        return Ok(result);
    }

    [HttpPost("backups/restore")]
    public async Task<IActionResult> RestoreBackup([FromBody] RestoreBackupRequest request, CancellationToken cancellationToken)
    {
        var result = await databaseBackupService.RestoreAsync(request.BackupFileName, request.ConfirmationPhrase, cancellationToken);
        return Ok(result);
    }

    public sealed class CreateBackupRequest
    {
        public string? Reason { get; set; }
    }

    public sealed class RestoreBackupRequest
    {
        public string BackupFileName { get; set; } = string.Empty;
        public string ConfirmationPhrase { get; set; } = string.Empty;
    }
}
