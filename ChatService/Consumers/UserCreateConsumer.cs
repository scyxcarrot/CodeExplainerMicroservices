using ChatService.DbContexts;
using ChatService.Models;

using CodeExplainerCommon.Contracts;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace ChatService.Consumers
{
    public class UserCreateConsumer(IDbContextFactory<ChatDbContext> dbContextFactory) : IConsumer<UserCreated>
    {
        public async Task Consume(ConsumeContext<UserCreated> context)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var userCreated = context.Message;
            var appUser = new AppUser()
            {
                ExternalId = userCreated.Id, 
                Chats = new List<Chat>()
            };
            await dbContext.AppUsers.AddAsync(appUser);

            await dbContext.SaveChangesAsync();
        }
    }
}
