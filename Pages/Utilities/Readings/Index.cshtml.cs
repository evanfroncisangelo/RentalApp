using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Utilities;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Utilities.Readings;

[Authorize]
public class IndexModel(
    IUtilityReadingService utilityReadingService,
    IUtilityCustomerService utilityCustomerService,
    RentalApp.Data.RentalDbContext dbContext) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? UtilityCustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? UtilityTypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty]
    public CreateReadingInputModel Input { get; set; } = new();

    public PagedResult<UtilityMeterReadingDto> PagedItems { get; private set; } = new();
    public List<SelectListItem> CustomerOptions { get; private set; } = [];
    public List<SelectListItem> UtilityTypeOptions { get; private set; } = [];
    public string? LastActionMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(Input, nameof(Input)))
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            var result = await utilityReadingService.CreateAndGenerateBillAsync(new CreateUtilityReadingRequestDto
            {
                UtilityCustomerId = Input.UtilityCustomerId,
                UtilityTypeId = Input.UtilityTypeId,
                ReadingDate = Input.ReadingDate,
                ReadingValue = Input.ReadingValue
            }, cancellationToken);

            LastActionMessage = $"Reading saved. Bill {result.Bill.BillingPeriod} generated amount {result.Bill.Amount:N2}."
                + (result.RecalculationTriggered ? " Backdated recalculation triggered." : string.Empty);
        }
        catch (AppValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var items = await utilityReadingService.GetAllAsync(UtilityCustomerId, UtilityTypeId, cancellationToken);
        PagedItems = PagedResult<UtilityMeterReadingDto>.Create(items, PageNumber, 10);

        var customers = await utilityCustomerService.GetAllAsync(null, cancellationToken);
        CustomerOptions = [new SelectListItem("All", "")];
        CustomerOptions.AddRange(customers
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), UtilityCustomerId == x.Id)));

        UtilityTypeOptions = [new SelectListItem("All", "")];
        UtilityTypeOptions.AddRange(await dbContext.UtilityTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), UtilityTypeId == x.Id))
            .ToListAsync(cancellationToken));

        if (Input.ReadingDate == default)
        {
            Input.ReadingDate = DateTime.UtcNow.Date;
        }
    }

    public class CreateReadingInputModel
    {
        [Required]
        public int UtilityCustomerId { get; set; }

        [Required]
        public int UtilityTypeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime ReadingDate { get; set; } = DateTime.UtcNow.Date;

        [Range(0, 1000000000)]
        public decimal ReadingValue { get; set; }
    }
}
