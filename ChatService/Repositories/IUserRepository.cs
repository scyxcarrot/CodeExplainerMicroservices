using ChatService.Models;
using CodeExplainerCommon.Responses;

namespace ChatService.Repositories
{
    public interface IUserRepository
    {
        public Task<ResponseResult> CreateUser(AppUser appUser);
        public Task<AppUser?> GetUserByExternalId(string externalUserId);
        public Task<AppUser?> GetUserById(Guid userId);
        public Task<ResponseResult> DeleteUserByExternalId(string externalUserId);
    }
}
