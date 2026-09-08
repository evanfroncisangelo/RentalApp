using RentalApp.Domain.Enums;

namespace RentalApp.Domain.Entities;

public class UtilityAuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public UtilityAuditActionType Action { get; set; } = UtilityAuditActionType.Create;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
