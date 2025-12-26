using CodeExplainerCommon.DTOs;

namespace ChatService.HttpClients
{
    public class UserServiceClient(
        HttpClient httpClient, 
        ILogger<UserServiceClient> logger) : IUserServiceClient
    {
        public async Task<UserSyncDTO?> GetUser(string userId)
        {
            try
            {
                // GetFromJsonAsync automatically handles status code checks and parsing
                var user = await httpClient.GetFromJsonAsync<UserSyncDTO>(
                    $"api/v1/UserService/User/{userId}");

                return user;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning("User {UserId} not found in UserService.", userId);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching user {UserId} from UserService.", userId);
                return null;
            }
        }
    }
}
