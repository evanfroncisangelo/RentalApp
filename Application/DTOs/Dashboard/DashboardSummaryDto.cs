namespace RentalApp.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public int AvailableUnits { get; set; }
    public int UnitsUnderMaintenance { get; set; }
    public int CurrentTenants { get; set; }

    public decimal ExpectedRent { get; set; }
    public decimal CollectedRent { get; set; }
    public decimal OutstandingRent { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }

    public decimal UtilityIncome { get; set; }
    public decimal UtilityExpenses { get; set; }
    public decimal OutstandingUtilityBills { get; set; }
    public decimal UtilityNet { get; set; }
    public decimal TotalUtilityConsumption { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public IReadOnlyList<ApartmentTenantDashboardRowDto> ApartmentTenantGrid { get; set; } = [];
    public IReadOnlyList<ExpenseDashboardRowDto> ExpenseGrid { get; set; } = [];
    public IReadOnlyList<UtilityCustomerDashboardRowDto> UtilityCustomerGrid { get; set; } = [];
}

public class ApartmentTenantDashboardRowDto
{
    public string RoomUnitLabel { get; set; } = string.Empty;
    public Dictionary<int, decimal> RentByMonth { get; set; } = [];
    public Dictionary<int, decimal> DepositByMonth { get; set; } = [];
    public decimal TotalRent { get; set; }
    public decimal TotalDeposit { get; set; }
}

public class UtilityCustomerDashboardRowDto
{
    public string CustomerLabel { get; set; } = string.Empty;
    public Dictionary<int, decimal> AmountByMonth { get; set; } = [];
    public decimal TotalAmount { get; set; }
}

public class ExpenseDashboardRowDto
{
    public string ExpenseCategory { get; set; } = string.Empty;
    public string PropertyUnit { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}
