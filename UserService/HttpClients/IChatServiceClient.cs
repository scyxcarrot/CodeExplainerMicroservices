using CodeExplainerCommon.DTOs;

namespace UserService.HttpClients
{
    public interface IChatServiceClient
    {
        Task<bool> NotifyUserCreated(UserCreatedDTO userCreatedDTO);
        Task<bool> NotifyUserDeleted(string userId);
    }
}
