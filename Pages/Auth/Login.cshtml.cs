using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Auth;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;

namespace RentalApp.Pages.Auth;

[AllowAnonymous]
public class LoginModel(IAuthService authService, RentalDbContext dbContext) : PageModel
{
    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public bool ShowRegisterButton { get; set; }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        Input.ReturnUrl = returnUrl;
        ShowRegisterButton = await dbContext.Users.AsNoTracking().CountAsync() <= 3;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ShowRegisterButton = await dbContext.Users.AsNoTracking().CountAsync(cancellationToken) <= 3;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var response = await authService.LoginAsync(new LoginRequestDto
            {
                Username = Input.Username,
                Password = Input.Password
            }, cancellationToken);

            await SignInAsync(response);
            return LocalRedirect(string.IsNullOrWhiteSpace(Input.ReturnUrl) ? "/Dashboard" : Input.ReturnUrl);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ErrorMessage = "Login request was canceled. Please try again.";
            return Page();
        }
        catch (AppUnauthorizedException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    private async Task SignInAsync(AuthResponseDto response)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, response.Username),
            new(ClaimTypes.GivenName, response.FirstName),
            new(ClaimTypes.Surname, response.LastName),
            new("access_token", response.Token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    public class LoginInputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
