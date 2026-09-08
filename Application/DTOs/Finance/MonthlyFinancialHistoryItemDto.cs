namespace RentalApp.Application.DTOs.Finance;

public class MonthlyFinancialHistoryItemDto
{
    public int Year { get; set; }
    public int Month { get; set; }
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
}
