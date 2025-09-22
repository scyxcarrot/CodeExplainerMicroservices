using CodeExplainerCommon.DTOs;

namespace IDSCodeExplainer.HttpClients
{
    public interface IChatServiceClient
    {
        Task<ChatReadDTO?> CreateChat(ChatCreateDTO chatCreateDTO);
    }
}
