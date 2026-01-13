using Erasmus_SSC.Dtos;
using Erasmus_SSC.Models;
using System.Threading.Tasks;

namespace Erasmus_SSC.Interfaces
{
    public interface IAuthService
    {

        Task<TokenResponseDto?> LoginUserAsync(LoginRequestDto request);
        Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken, string ipAddress);
        Task<bool> ChangeUserPasswordAsync(string email, string newPassword);
        Task<RefreshToken> CreateRefreshTokenAsync(string ipAddress, string device);

    }
}
