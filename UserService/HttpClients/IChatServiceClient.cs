using CodeExplainerCommon.DTOs;

namespace UserService.HttpClients
{
    public interface IChatServiceClient
    {
        Task NotifyUserCreated(UserCreatedDTO userCreatedDTO);
    }
}
