using Erasmus_SSC.Dtos;

namespace Erasmus_SSC.Interfaces
{
    public interface IUserAdminService
    {
        Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default);
        Task<UserDto> CreateUserAsync(RegisterRequestDto dto, CancellationToken ct = default);
        Task<bool> DeleteUserAsync(int userId, CancellationToken ct = default);
    }
}
