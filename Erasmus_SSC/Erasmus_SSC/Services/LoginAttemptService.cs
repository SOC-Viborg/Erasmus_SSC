using Erasmus_SSC.Interfaces;
using Erasmus_SSC.Models;
using Microsoft.Extensions.Caching.Memory;

namespace API.Services
{
    /// <summary>
    /// Service for tracking login attempts and handling lockout logic.
    /// </summary>
    public class LoginAttemptService : ILoginAttemptService
    {
        private readonly IMemoryCache _cache;
        /// <summary>
        /// Maximum allowed failed login attempts before lockout.
        /// </summary>
        private readonly int _maxAttempts = 5;
        /// <summary>
        /// Lockout duration in seconds after exceeding max attempts.
        /// </summary>
        private readonly int _lockoutSeconds = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginAttemptService"/> class.
        /// </summary>
        /// <param name="cache">Memory cache for storing login attempt info.</param>
        public LoginAttemptService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Checks if the user is currently locked out based on their email.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <returns>True if locked out, otherwise false.</returns>
        public bool IsLockedOut(string email)
        {
            var info = GetLoginAttemptInfo(email);
            // User is locked out if LockoutEnd is set and in the future
            return info != null && info.LockoutEnd.HasValue && info.LockoutEnd > DateTime.UtcNow;
        }

        /// <summary>
        /// Records a failed login attempt for the specified email.
        /// Locks out the user if max attempts are reached.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <returns>Number of attempts left before lockout.</returns>
        public int RecordFailedAttempt(string email)
        {
            var info = GetOrCreateAttemptInfo(email);
            info.FailedAttempts++;

            // Lockout if failed attempts exceed max allowed
            if (info.FailedAttempts >= _maxAttempts)
            {
                info.LockoutEnd = DateTime.UtcNow.AddSeconds(_lockoutSeconds);
            }

            SetLoginAttemptInfo(email, info);

            // Return number of attempts left before lockout
            var remaining = _maxAttempts - info.FailedAttempts;
            return remaining;
        }

        /// <summary>
        /// Gets the remaining lockout time in seconds for the specified email.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <returns>Seconds remaining in lockout period, or 0 if not locked out.</returns>
        public int GetRemainingLockoutSeconds(string email)
        {
            var info = GetLoginAttemptInfo(email);
            if (info == null || !info.LockoutEnd.HasValue)
                return 0;

            var remaining = (int)(info.LockoutEnd.Value - DateTime.UtcNow).TotalSeconds;
            return Math.Max(0, remaining);
        }

        /// <summary>
        /// Retrieves the login attempt info for the specified email.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <returns>LoginAttemptInfo if found, otherwise null.</returns>
        public LoginAttemptInfo? GetLoginAttemptInfo(string email)
        {
            _cache.TryGetValue(email, out LoginAttemptInfo? info);
            return info;
        }

        /// <summary>
        /// Clears login attempt info after a successful login.
        /// </summary>
        /// <param name="email">User's email address.</param>
        public void RecordSuccessfulLogin(string email)
        {
            _cache.Remove(email);
        }

        /// <summary>
        /// Gets existing or creates new LoginAttemptInfo for the specified email.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <returns>LoginAttemptInfo instance.</returns>
        private LoginAttemptInfo GetOrCreateAttemptInfo(string email)
        {
            var info = GetLoginAttemptInfo(email);
            if (info == null)
            {
                info = new LoginAttemptInfo();
                SetLoginAttemptInfo(email, info);
            }
            return info;
        }

        /// <summary>
        /// Stores the LoginAttemptInfo in cache for the specified email.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <param name="info">LoginAttemptInfo to store.</param>
        private void SetLoginAttemptInfo(string email, LoginAttemptInfo info)
        {
            // Cache entry expires after 60 minutes
            _cache.Set(email, info, TimeSpan.FromMinutes(60));
        }
    }
}