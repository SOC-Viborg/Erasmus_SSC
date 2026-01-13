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

        var accessToken = _jwtService.CreateToken(user);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
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

        var newAccessToken = _jwtService.CreateToken(user);

        return new TokenResponseDto
        {
            AccessToken = newAccessToken,
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

//using Erasmus_SSC.Data;
//using Erasmus_SSC.Interfaces;
//using Erasmus_SSC.Dtos;
//using Erasmus_SSC.Models;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Security.Cryptography;

//namespace API.Services;

///// <summary>
///// Provides authentication and user account management logic for Blazor WebAssembly applications.
///// Handles registration, login, token management, and password changes.
///// </summary>
//public class AuthService : IAuthService
//{
//    private readonly ApplicationDbContext _context;

//    private readonly ILogger<AuthService> _logger;
//    private readonly IJWTService _jwtService;
//    private readonly IHttpContextAccessor _httpContextAccessor;


//    /// <summary>
//    /// Initializes a new instance of the <see cref="AuthService"/> class.
//    /// </summary>
//    /// <param name="context">The database context for user and token data.</param>
//    /// <param name="logger">Logger for authentication events and errors.</param>
//    /// <param name="jwtService">Service for generating JWT tokens.</param>
//    /// <param name="httpContextAccessor">Accessor for HTTP context, used for IP and device info.</param>

//    public AuthService(ApplicationDbContext context, ILogger<AuthService> logger, IJWTService jwtService, IHttpContextAccessor httpContextAccessor)
//    {
//        _context = context;
//        _logger = logger;
//        _jwtService = jwtService;
//        _httpContextAccessor = httpContextAccessor;

//    }


//    public async Task<TokenResponseDto?> LoginUserAsync(LoginRequestDto request)
//    {
//        var httpContext = _httpContextAccessor.HttpContext;
//        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
//        var device = httpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;


//        var user = await _context.Users

//        .FirstOrDefaultAsync(u => u.UserName == request.UserName);

//        if (user == null)
//        {
//            return null; // User not found
//        }

//        if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
//        == PasswordVerificationResult.Failed)
//        {
//            return null;
//        }


//        string accessToken = _jwtService.CreateToken(user);
//        var refreshToken = await CreateRefreshTokenAsync(ipAddress, device);

//        // Only query tokens that are not revoked and not expired
//        var now = DateTime.UtcNow.AddHours(2);
//        var existingTokens = await _context.RefreshTokens
//            .Where(rt => rt.UserId == user.Id && rt.Expires > now && rt.Revoked == null)
//            .ToListAsync();

//        if (existingTokens.Count > 0)
//        {
//            foreach (var token in existingTokens)
//            {
//                token.Revoked = now;
//                token.ReplacedByToken = refreshToken.Token;
//            }
//        }

//        foreach (var token in existingTokens)
//        {
//            token.Revoked = DateTime.UtcNow;
//            token.ReplacedByToken = refreshToken.Token;
//        }

//        user.RefreshTokens ??= new List<RefreshToken>();
//        user.RefreshTokens.Add(refreshToken);

//        await _context.SaveChangesAsync();

//        return new TokenResponseDto
//        {
//            AccessToken = accessToken,
//            RefreshToken = refreshToken.Token,
//        };
//    }

//    public async Task<RefreshToken?> CreateRefreshTokenAsync(string ipAddress, string device)
//    {
//        return new RefreshToken
//        {
//            Token = GenerateRefreshToken(),
//            CreatedByIp = ipAddress,a
//            Device = device,
//            Created = DateTime.UtcNow.AddHours(2),
//            Expires = DateTime.UtcNow.AddDays(7)
//        };
//    }

//    /// <summary>
//    /// Generates a secure random string to be used as a refresh token.
//    /// </summary>
//    /// <returns>A base64-encoded random string suitable for use as a refresh token.</returns>
//    public string GenerateRefreshToken()
//    {
//        var randomNumber = new byte[64];
//        using var rng = RandomNumberGenerator.Create();
//        rng.GetBytes(randomNumber);
//        return Convert.ToBase64String(randomNumber);
//    }

//    /// <summary>
//    /// Refreshes the JWT access token using a valid refresh token.
//    /// Used by authenticated users to renew their session.
//    /// </summary>
//    /// <param name="token">The refresh token string.</param>
//    /// <param name="ipAddress">The IP address from which the refresh is requested.</param>
//    /// <param name="device">A string identifying the user's device.</param>
//    /// <returns>
//    /// A <see cref="TokenResponseDto"/> containing new access and refresh tokens if successful; otherwise, <c>null</c>.
//    /// </returns>
//    public async Task<TokenResponseDto?> RefreshTokenAsync(string token, string ipAddress, string device)
//    {
//        // Find the existing refresh token
//        var existingToken = await _context.RefreshTokens
//            .Include(rt => rt.User)

//            .FirstOrDefaultAsync(rt => rt.Token == token);

//        // If invalid, expired, or revoked, reject
//        if (existingToken == null || existingToken.Expires <= DateTime.UtcNow.AddHours(2)
//            || existingToken.Revoked != null)
//            return null;

//        // Revoke the old token
//        existingToken.Revoked = DateTime.UtcNow.AddHours(2);

//        // Create a new refresh token and save
//        var newRefreshToken = await CreateRefreshTokenAsync(ipAddress, device);
//        newRefreshToken.UserId = existingToken.UserId;

//        _context.RefreshTokens.Add(newRefreshToken);

//        // Generate a new JWT access token
//        var accessToken = _jwtService.CreateToken(existingToken.User);
//        await _context.SaveChangesAsync();

//        return new TokenResponseDto
//        {
//            AccessToken = accessToken,
//            RefreshToken = newRefreshToken.Token
//        };
//    }

//    /// <summary>
//    /// Changes the password for a user identified by their email address.
//    /// Intended for administrative password resets or recovery scenarios.
//    /// Should be called by users with administrative privileges.
//    /// </summary>
//    /// <param name="userEmail">The email address of the user whose password will be changed.</param>
//    /// <param name="newPassword">The new password to set for the user.</param>
//    /// <returns>
//    /// <c>true</c> if the password was changed successfully; otherwise, <c>false</c>.
//    /// </returns>
//    public async Task<bool> ChangeUserPasswordAsync(string userEmail, string newPassword)
//    {
//        try
//        {
//            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
//            if (user == null)
//            {
//                _logger.LogWarning("User with email {Email} not found.", userEmail);
//                return false;
//            }
//            var passwordHasher = new PasswordHasher<User>();
//            user.PasswordHash = passwordHasher.HashPassword(user, newPassword);

//            await _context.SaveChangesAsync();
//            return true;
//        }
//        catch (DbUpdateException ex)
//        {
//            _logger.LogError(ex, "Error updating user password for {Email}", userEmail);
//            return false;
//        }
//    }


//}