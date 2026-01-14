using Erasmus_SSC.Data;
using Erasmus_SSC.Dtos;
using Erasmus_SSC.Interfaces;
using Erasmus_SSC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace API.Services;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IJWTService _jwtService;

    public AuthService(
        ApplicationDbContext context,
        ILogger<AuthService> logger,
        IJWTService jwtService)
    {
        _context = context;
        _logger = logger;
        _jwtService = jwtService;
    }

    public async Task<TokenResponseDto?> LoginUserAsync(LoginRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        
        var identifier = request.UserName.Trim();

        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u =>
                u.UserName == identifier ||
                u.Email == identifier);

        if (user is null)
        {
            _logger.LogWarning("Login failed: user not found. Identifier={Identifier}", identifier);
            return null;
        }


        var hasher = new PasswordHasher<User>();
        var passwordCheck = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (passwordCheck != PasswordVerificationResult.Success)
        {
            _logger.LogWarning("Login failed: invalid password. UserId={UserId}", user.Id);
            return null;
        }

        var now = DateTime.UtcNow;

        
        var newRefreshToken = await CreateRefreshTokenAsync(ipAddress: "unknown", device: "unknown");
        newRefreshToken.UserId = user.Id;

        foreach (var t in user.RefreshTokens.Where(IsActive))
        {
            t.Revoked = now;
            t.ReplacedByToken = newRefreshToken.Token;
        }

        user.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var accessToken = _jwtService.CreateTokenAsync(user);

        return new TokenResponseDto
        {
            AccessToken = await _jwtService.CreateTokenAsync(user),
            RefreshToken = newRefreshToken.Token
        };
    }

    public async Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var tokenEntity = await _context.RefreshTokens
      .Include(rt => rt.User)
          .ThenInclude(u => u.RefreshTokens)
      .Include(rt => rt.User)
          .ThenInclude(u => u.Role)
      .SingleOrDefaultAsync(rt => rt.Token == refreshToken);


        if (tokenEntity is null)
        {
            _logger.LogWarning("Refresh failed: token not found.");
            return null;
        }

        if (!IsActive(tokenEntity))
        {
            _logger.LogWarning("Refresh failed: token not active. TokenId={TokenId} UserId={UserId}", tokenEntity.Id, tokenEntity.UserId);
            return null;
        }

        var user = tokenEntity.User;
        if (user is null)
        {
            _logger.LogWarning("Refresh failed: token has no user. TokenId={TokenId}", tokenEntity.Id);
            return null;
        }

        var now = DateTime.UtcNow;

       
        var newRefreshToken = await CreateRefreshTokenAsync(ipAddress, device: tokenEntity.Device ?? "unknown");
        newRefreshToken.UserId = user.Id;

        tokenEntity.Revoked = now;
        tokenEntity.ReplacedByToken = newRefreshToken.Token;

        user.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var newAccessToken = _jwtService.CreateTokenAsync(user);

        return new TokenResponseDto
        {
            AccessToken = await _jwtService.CreateTokenAsync(user),
            RefreshToken = newRefreshToken.Token
        };
    }

    public Task<RefreshToken> CreateRefreshTokenAsync(string ipAddress, string device)
    {
       
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);

        var now = DateTime.UtcNow;

        var refreshToken = new RefreshToken
        {
            Token = token,
            Created = now,
            Expires = now.AddDays(14),              
            CreatedByIp = ipAddress ?? string.Empty,
            Device = string.IsNullOrWhiteSpace(device) ? "Unknown device" : device
        };

        return Task.FromResult(refreshToken);
    }

    public async Task<bool> ChangeUserPasswordAsync(string email, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
            return false;

        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email.Trim());
        if (user is null)
            return false;

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, newPassword);

        await _context.SaveChangesAsync();
        return true;
    }

    private static bool IsActive(RefreshToken token)
        => token.Revoked is null && token.Expires > DateTime.UtcNow;
}

