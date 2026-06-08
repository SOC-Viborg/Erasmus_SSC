using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Services;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Configuration;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Erasmus_SSC.Tests.Services
{
    public class LoginAttemptServiceTests
    {
        [Fact]

        public void RecordFailedAttempt_ShouldDecreaseAttemptsLeft()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new LoginAttemptService(cache);

            var attemptsLeft = service.RecordFailedAttempt("test@example.com");

            Assert.Equal(4, attemptsLeft);


        }

        [Fact]

        public void IsLockedOut_ShouldReturnTrue_AfterFiveFailedAttempts()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new LoginAttemptService(cache);

            service.RecordFailedAttempt("test@example.com");
            service.RecordFailedAttempt("test@example.com");
            service.RecordFailedAttempt("test@example.com");
            service.RecordFailedAttempt("test@example.com");
            service.RecordFailedAttempt("test@example.com");

            Assert.True(service.IsLockedOut("testc@examole.com"));
        }

        [Fact]
        public void RecordSuccessfulLogin_ShouldClearLockoutState()
            {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new LoginAttemptService(cache);

            service.RecordFailedAttempt("test@example.com");
            service.RecordSuccessfulLogin("test@example.com");

            Assert.False(service.IsLockedOut("test@example.com"));
            Assert.Null(service.GetLoginAttemptInfo("test@example.com"));
        }

        [Fact]
        public void NewEmail_ShouldNotBeLockedOut()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new LoginAttemptService(cache);
            var result = service.IsLockedOut("new@example.com");
            Assert.False(result);
        }
    }
}