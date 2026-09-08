using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RentalApp.Application.DTOs.Auth;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;

namespace RentalApp.Pages.Auth;

[AllowAnonymous]
public class ForgotPasswordModel(IAuthService authService) : PageModel
{
    [BindProperty]
    public ForgotPasswordInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await authService.ForgotPasswordAsync(new ForgotPasswordRequestDto
            {
                Username = Input.Username,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                NewPassword = Input.NewPassword,
                ConfirmPassword = Input.ConfirmPassword
            }, cancellationToken);

            SuccessMessage = "Password updated successfully. You can now login.";
            Input = new ForgotPasswordInputModel();
            return Page();
        }
        catch (AppUnauthorizedException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public class ForgotPasswordInputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and confirm password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
