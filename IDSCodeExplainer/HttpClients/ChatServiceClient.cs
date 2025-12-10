using System.Text;
using System.Text.Json;
using CodeExplainerCommon.DTOs;

namespace IDSCodeExplainer.HttpClients
{
    public class ChatServiceClient(
        HttpClient httpClient, 
        ILogger<ChatServiceClient> logger) : IChatServiceClient
    {
        public async Task<ChatReadDTO?> CreateChat(ChatCreateDTO chatCreateDTO)
        {
            var httpContent = new StringContent(
                JsonSerializer.Serialize(chatCreateDTO), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(
                "api/v1/ChatService/Chat",
                httpContent);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Success: Chat was created on ChatService");
                var chatReadDTO = await response.Content.ReadFromJsonAsync<ChatReadDTO>();
                return chatReadDTO;
            }

            logger.LogError("Error: {ResponseStatusCode}", response.StatusCode);
            return null;
        }

        public async Task<ChatReadDTO?> GetChatMessages(Guid chatId)
        {
            var response = await httpClient.GetAsync(
                $"api/v1/ChatService/Chat/{chatId}");

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Success: Chat was created on ChatService");
                var chatReadDTO = await response.Content.ReadFromJsonAsync<ChatReadDTO>();
                return chatReadDTO;
            }

            logger.LogError("Error: {ResponseStatusCode}", response.StatusCode);
            return null;
        }
    }
}
