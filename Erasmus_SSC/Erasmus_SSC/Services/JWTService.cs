using Erasmus_SSC.Data;
using Erasmus_SSC.Interfaces;
using Erasmus_SSC.Models;

using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Erasmus_SSC.Services;

/// <summary>
/// Provides functionality for generating JWT access tokens for both application and Active Directory users.
/// Used for authentication and session management in Blazor WebAssembly applications.
/// </summary>
public class JWTService : IJWTService
{
    private readonly ILogger<JWTService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="JWTService"/> class.
    /// </summary>
    /// <param name="logger">Logger for recording token generation events and errors.</param>
    /// <param name="configuration">Application configuration for retrieving JWT settings.</param>
    public JWTService(ILogger<JWTService> logger, IConfiguration configuration, ApplicationDbContext appDBContext)
    {
        _logger = logger;
        _configuration = configuration;
        _db = appDBContext;
    }

    /// <summary>
    /// Generates a JWT access token for a standard application user.
    /// The token includes user claims and roles for secure authentication.
    /// </summary>
    /// <param name="user">The application user for whom the token is generated.</param>
    /// <returns>
    /// A JWT token string representing the authenticated user.
    /// </returns>
    public string CreateToken(User user)
    {
        try
        {
            return GenerateToken(new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            //new Claim(ClaimTypes.Role, user.UserRole.RoleName.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),

        });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating token for user {UserId}", user.Id);
            throw;
        }
    }
    public string GenerateToken(IEnumerable<Claim> claims)
    {
        var jwtSection = _configuration.GetSection("Jwt");

        var secretKey = jwtSection["Key"];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Jwt:Key is missing in appsettings.json");

        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];

       
        var accessTokenMinutes = jwtSection.GetValue<int?>("AccessTokenMinutes") ?? 15;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            SigningCredentials = creds
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }


}