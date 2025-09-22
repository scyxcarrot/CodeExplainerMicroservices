using System.Text;
using System.Text.Json;
using CodeExplainerCommon.DTOs;

namespace UserService.HttpClients
{
    public class ChatServiceClient(
        HttpClient httpClient, 
        ILogger<ChatServiceClient> logger) : IChatServiceClient
    {
        public async Task NotifyUserCreated(UserCreatedDTO userCreatedDTO)
        {
            var httpContent = new StringContent(
                JsonSerializer.Serialize(userCreatedDTO), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(
                "api/v1/User",
                httpContent);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Success: User was sent to ChatService");
            }
            else
            {
                logger.LogError("Error: {ResponseStatusCode}", response.StatusCode);
            }
        }
    }
}
