namespace RentalApp.Domain.Enums;

public enum UtilityRecalculationBatchStatus
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    RolledBack = 5
}
