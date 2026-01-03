using ChatService.DbContexts;
using ChatService.HttpClients;
using ChatService.Models;
using CodeExplainerCommon.DTOs;
using CodeExplainerCommon.Responses;

using Microsoft.EntityFrameworkCore;

namespace ChatService.Repositories
{
    public class UserRepository(
        IDbContextFactory<ChatDbContext> dbContextFactory, 
        IUserServiceClient userServiceClient) : IUserRepository
    {
        public async Task<AppUser> GetOrCreateUserByExternalId(string externalUserId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var appUserFound = await dbContext.AppUsers
                .AsNoTracking()
                .Include(user => user.Chats)
                .ThenInclude(chat=>chat.Messages)
                .FirstOrDefaultAsync(user => user.ExternalId == externalUserId);

            if (appUserFound != null)
            {
                return appUserFound;
            }

            // Call from UserService to pull the data down
            UserSyncDTO? userSyncDTO = await userServiceClient.GetUser(externalUserId);
            if (userSyncDTO == null)
            {
                return null;
            }
            
            // create the app user and then since its a new user, output empty chat list
            AppUser appUser = new AppUser()
            {
                ExternalId = userSyncDTO.Id,
                Chats = new List<Chat>(),
            };

            await dbContext.AppUsers.AddAsync(appUser);
            await dbContext.SaveChangesAsync();

            return appUser;
        }
    }
}
