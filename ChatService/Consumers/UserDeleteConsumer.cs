using ChatService.DbContexts;

using CodeExplainerCommon.Contracts;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace ChatService.Consumers
{
    public class UserDeleteConsumer(IDbContextFactory<ChatDbContext> dbContextFactory) : IConsumer<UserDeleted>
    {
        public async Task Consume(ConsumeContext<UserDeleted> context)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var userDeleted = context.Message;
            var appUser = await dbContext.AppUsers.FirstOrDefaultAsync(
                appUser => appUser.ExternalId == userDeleted.Id);

            if (appUser is not null)
            {
                dbContext.AppUsers.Remove(appUser);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
