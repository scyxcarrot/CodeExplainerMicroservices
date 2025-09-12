using ChatService.DbContexts;
using ChatService.Models;

using CodeExplainerCommon.Responses;

using Microsoft.EntityFrameworkCore;

namespace ChatService.Repositories
{
    public class UserRepository(IDbContextFactory<ChatDbContext> dbContextFactory) : IUserRepository
    {
        public async Task<ResponseResult> CreateUser(AppUser appUser)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.AppUsers.AddAsync(appUser);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true, 
                $"User with external Id = {appUser.ExternalId} created");
        }

        public async Task<AppUser?> GetUserByExternalId(string externalUserId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var appUserFound = await dbContext.AppUsers
                .AsNoTracking()
                .Include(user => user.Chats)
                .FirstOrDefaultAsync(user => user.ExternalId == externalUserId);
            return appUserFound;
        }

        public async Task<ResponseResult> DeleteUserByExternalId(string externalUserId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var appUserFound = await dbContext.AppUsers
                .FirstOrDefaultAsync(user => user.ExternalId == externalUserId);
            if (appUserFound == null)
            {
                return new ResponseResult(false,
                    $"User with external Id = {externalUserId} not found");
            }

            dbContext.AppUsers.Remove(appUserFound);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true,
                $"User with external Id = {externalUserId} deleted");
        }
    }
}
