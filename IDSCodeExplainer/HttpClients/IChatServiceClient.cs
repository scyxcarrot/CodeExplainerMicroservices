using CodeExplainerCommon.DTOs;

namespace IDSCodeExplainer.HttpClients
{
    public interface IChatServiceClient
    {
        Task<ChatReadDTO?> CreateChat(ChatCreateDTO chatCreateDTO);

        Task<ChatReadDTO?> GetChatMessages(Guid chatId);
    }
}
