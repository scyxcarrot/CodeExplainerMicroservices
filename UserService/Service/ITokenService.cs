using UserService.Models;

namespace UserService.Service
{
    public interface ITokenService
    {
        public Task<string> CreateToken(AppUser user);

        public Task<string> CreateRefreshToken(AppUser user);

        public Task<string> GetUserIdFromRefreshToken(string? refreshToken);

        public Task<bool> DeleteRefreshToken(string userId);
    }
}
