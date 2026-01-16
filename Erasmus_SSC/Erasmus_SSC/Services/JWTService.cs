using Erasmus_SSC.Data;
using Erasmus_SSC.Interfaces;
using Erasmus_SSC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Erasmus_SSC.Services;

public class JWTService : IJWTService
{
    private readonly ILogger<JWTService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _db;

    public JWTService(ILogger<JWTService> logger, IConfiguration configuration, ApplicationDbContext db)
    {
        _logger = logger;
        _configuration = configuration;
        _db = db;
    }

    public async Task<string> CreateTokenAsync(User user, CancellationToken ct = default)
    {
        try
        {
            var roleName = user.Role?.RoleName;
            roleName ??= "User"; // fallback


            if (string.IsNullOrWhiteSpace(roleName))
            {
                roleName = await _db.UserRoles
                    .AsNoTracking()
                    .Where(r => r.Id == user.RoleId)
                    .Select(r => r.RoleName)
                    .FirstOrDefaultAsync(ct);
            }

            roleName ??= "User";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(ClaimTypes.Name, user.UserName ?? ""),
                new(ClaimTypes.Role, roleName),
            };

            return GenerateToken(claims); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating token for user {UserId}", user.Id);
            throw;
        }
    }

    private string GenerateToken(IEnumerable<Claim> claims)
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
