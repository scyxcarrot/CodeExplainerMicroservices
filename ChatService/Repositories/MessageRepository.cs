using ChatService.DbContexts;
using ChatService.Models;

using CodeExplainerCommon.Responses;

using Microsoft.EntityFrameworkCore;

namespace ChatService.Repositories
{
    public class MessageRepository(IDbContextFactory<ChatDbContext> dbContextFactory) : 
        IMessageRepository
    {
        public async Task<ResponseResult> CreateMessage(Message message)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();

            var maxMessageOrder = await dbContext.Messages
                .Where(m => m.ChatId == message.ChatId)
                .MaxAsync(m => (long?)m.MessageOrder) ?? 0;

            message.TimeStamp = DateTime.Now;
            message.MessageOrder = maxMessageOrder + 1;
            
            await dbContext.Messages.AddAsync(message);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true,
                $"Message created at {message.TimeStamp}");
        }

        public async Task<IEnumerable<Message>> GetAllMessagesByChatId(Guid chatId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var messages = dbContext.Messages.AsNoTracking()
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.TimeStamp);
            return messages;
        }

        public async Task<ResponseResult> UpdateMessage(Message message)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var messageFound = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == message.Id);
            if (messageFound == null)
            {
                return new ResponseResult(false,
                    $"Message with Id = {message.Id} not found");
            }

            messageFound.ChatRole = message.ChatRole;
            messageFound.TextMessage = message.TextMessage;

            dbContext.Messages.Update(messageFound);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true,
                $"Message with Id = {message.Id} updated");
        }

        public async Task<ResponseResult> DeleteMessage(Guid messageId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var messageFound = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);
            if (messageFound == null)
            {
                return new ResponseResult(false,
                    $"Message with Id = {messageId} not found");
            }

            dbContext.Messages.Remove(messageFound);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true,
                $"Message with Id = {messageId} deleted");
        }
    }
}
