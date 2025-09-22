using ChatService.DbContexts;
using ChatService.Mappings;

using CodeExplainerCommon.DTOs;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace ChatService.Consumers
{
    public class ChatCreateConsumer(IDbContextFactory<ChatDbContext> dbContextFactory) : IConsumer<MessageCreateDTO>
    {
        public async Task Consume(ConsumeContext<MessageCreateDTO> context)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var messageCreateDTO = context.Message;
            await dbContext.Messages.AddAsync(messageCreateDTO.ToModel());

            await dbContext.SaveChangesAsync();
        }
    }
}
