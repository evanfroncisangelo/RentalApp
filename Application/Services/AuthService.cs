using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalApp.Application.DTOs.Auth;
using RentalApp.Application.Exceptions;
using RentalApp.Application.Interfaces;
using RentalApp.Data;
using RentalApp.Domain.Entities;

namespace RentalApp.Application.Services;

public class AuthService(
    RentalDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

        if (existingUser is not null)
        {
            throw new AppValidationException("Username is already taken.");
        }

        var user = new User
        {
            Username = request.Username.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var (token, expiresAtUtc) = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

        if (user is null)
        {
            throw new AppUnauthorizedException("Invalid username or password.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            throw new AppUnauthorizedException("Invalid username or password.");
        }

        var (token, expiresAtUtc) = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, cancellationToken);

        if (user is null ||
            !string.Equals(user.FirstName, request.FirstName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(user.LastName, request.LastName, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppUnauthorizedException("User identity could not be verified.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
