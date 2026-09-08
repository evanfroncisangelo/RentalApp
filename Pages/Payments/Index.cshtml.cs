using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.Common;
using RentalApp.Application.DTOs.Payments;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Payments;

[Authorize]
public class IndexModel(IPaymentService paymentService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<PaymentDto> PagedItems { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = await paymentService.GetAllAsync(null, cancellationToken);
        PagedItems = PagedResult<PaymentDto>.Create(items, PageNumber, 10);
    }
}
