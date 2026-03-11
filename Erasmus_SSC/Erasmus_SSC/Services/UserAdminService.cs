using Erasmus_SSC.Client.Dtos;
using Erasmus_SSC.Data;
using Erasmus_SSC.Dtos;
using Erasmus_SSC.Interfaces;
using Erasmus_SSC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erasmus_SSC.Services
{
    public class UserAdminService : IUserAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly ILogger<UserAdminService> _logger;

       
        private const int AdminRoleId = 1;
        private const int DefaultUserRoleId = 2;

        public UserAdminService(
            ApplicationDbContext context,
            ILogger<UserAdminService> logger)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<UserDto> CreateUserAsync(RegisterRequestDto dto, CancellationToken ct = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var userName = dto.UserName?.Trim();
            var email = dto.Email?.Trim();

            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("UserName is required.", nameof(dto.UserName));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(dto.Email));

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters.", nameof(dto.Password));

            var normalizedUserName = userName.ToLowerInvariant();
            var normalizedEmail = email.ToLowerInvariant();

            var exists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.UserName.ToLower() == normalizedUserName ||
                    u.Email.ToLower() == normalizedEmail, ct);

            if (exists)
                throw new InvalidOperationException("User with the same username or email already exists.");

            var user = new User
            {
                UserName = userName,
                Email = normalizedEmail,
                RoleId = DefaultUserRoleId
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            
            var roleName = await _context.UserRoles
                .AsNoTracking()
                .Where(r => r.Id == user.RoleId)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync(ct) ?? "User";

            _logger.LogInformation("Admin created user {UserId} ({UserName})", user.Id, user.UserName);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                UserRole = roleName
            };
        }
        public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .OrderBy(u => u.Id)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    UserRole = u.Role != null ? u.Role.RoleName : "User"
                })
                .ToListAsync(ct);
        }

        public async Task<bool> DeleteUserAsync(int userId, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) return false;

            
            if (user.RoleId == AdminRoleId)
                throw new InvalidOperationException("Admin user cannot be deleted via this endpoint.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Admin deleted user {UserId}", userId);
            return true;
        }

        private async Task<int> GetRoleIdByNameAsync(string roleName, CancellationToken ct)
        {
            var normalized = roleName.Trim();
            var id = await _context.UserRoles
                .AsNoTracking()
                .Where(r => r.RoleName == normalized)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            if (id != 0) return id;

            
            var role = new UserRole { RoleName = normalized };
            _context.UserRoles.Add(role);
            await _context.SaveChangesAsync(ct);
            return role.Id;
        }
        public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequestDto dto, CancellationToken ct = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null) throw new InvalidOperationException("User not found.");

            var userName = dto.UserName?.Trim();
            var email = dto.Email?.Trim();

            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("UserName is required.", nameof(dto.UserName));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(dto.Email));

            // unique check
            var normalizedUserName = userName.ToLowerInvariant();
            var normalizedEmail = email.ToLowerInvariant();

            var exists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id != userId &&
                               (u.UserName.ToLower() == normalizedUserName ||
                                u.Email.ToLower() == normalizedEmail), ct);

            if (exists)
                throw new InvalidOperationException("Another user with the same username or email already exists.");

            user.UserName = userName;
            user.Email = normalizedEmail;

            //// role
            //var roleName = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role.Trim();
            //user.RoleId = await GetRoleIdByNameAsync(roleName, ct);

            //  password reset
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                if (dto.Password.Length < 6)
                    throw new ArgumentException("Password must be at least 6 characters.", nameof(dto.Password));

                user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
            }

            await _context.SaveChangesAsync(ct);

            // reload role name
            //var finalRoleName = await _context.UserRoles
            //    .AsNoTracking()
            //    .Where(r => r.Id == user.RoleId)
            //    .Select(r => r.RoleName)
            //    .FirstOrDefaultAsync(ct) ?? "User";

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
               
            };
        }


    }
}
