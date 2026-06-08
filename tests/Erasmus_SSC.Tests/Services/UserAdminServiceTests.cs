using API.Services;
using Erasmus_SSC.Data;
using Erasmus_SSC.Dtos;
using Erasmus_SSC.Models;
using Erasmus_SSC.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Erasmus_SSC.Tests.Services;

public class UserAdminServiceTests
{
    [Fact]
    public async Task CreateUserAsync_ShouldCreateUserWithDefaultRole()
    {
        var option = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ApplicationDbContext(option);
        db.UserRoles.Add(new UserRole { Id = 2, RoleName = "User" });

        await db.SaveChangesAsync();

        var service = new UserAdminService(
            db,
            NullLogger<UserAdminService>.Instance);

        var dto = new Erasmus_SSC.Dtos.RegisterRequestDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        var created = await service.CreateUserAsync(dto);

        var userFromDb = await db.Users.SingleOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(userFromDb);
        Assert.Equal("testuser", created.UserName);
        Assert.Equal("test@example.com", created.Email);
        Assert.Equal("User", created.UserRole);
        Assert.False(string.IsNullOrWhiteSpace(userFromDb.PasswordHash));

        Assert.Equal(created.Id, userFromDb.Id);
    }

    [Fact]
    public void IsLockedOut_ShouldReturnFalse_AfterOneFailedAttempt()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new LoginAttemptService(cache);

        service.RecordFailedAttempt("test@example.com");

        var result = service.IsLockedOut("test@example.com");

        Assert.False(result);
    }

 
}
    

    

