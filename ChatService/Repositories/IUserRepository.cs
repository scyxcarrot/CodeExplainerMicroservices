using ChatService.Models;
using CodeExplainerCommon.Responses;

namespace ChatService.Repositories
{
    public interface IUserRepository
    {
        public Task<AppUser> GetOrCreateUserByExternalId(string externalUserId);
    }
}
