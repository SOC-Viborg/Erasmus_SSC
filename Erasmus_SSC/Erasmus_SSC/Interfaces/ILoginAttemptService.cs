using Erasmus_SSC.Models;

namespace Erasmus_SSC.Interfaces
{
    public interface ILoginAttemptService
    {
        bool IsLockedOut(string email);
        int RecordFailedAttempt(string email);
        int GetRemainingLockoutSeconds(string email);
        LoginAttemptInfo? GetLoginAttemptInfo(string email);
        void RecordSuccessfulLogin(string email);
    }
}
