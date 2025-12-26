using CodeExplainerCommon.DTOs;

namespace ChatService.HttpClients
{
    public interface IUserServiceClient
    {
        Task<UserSyncDTO?> GetUser(string userId);
    }
}
