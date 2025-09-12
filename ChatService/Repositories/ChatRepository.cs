using ChatService.DbContexts;
using ChatService.Models;

using CodeExplainerCommon.Responses;

using Microsoft.EntityFrameworkCore;

namespace ChatService.Repositories
{
    public class ChatRepository(IDbContextFactory<ChatDbContext> dbContextFactory) : IChatRepository
    {
        public async Task<ResponseResult> CreateChat(Chat chat)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.Chats.AddAsync(chat);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true,
                $"Chat {chat.Title} created");
        }

        public async Task<IEnumerable<Chat>> GetAllChatsByUserExternalId(string userExternalId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var userId = await dbContext.AppUsers
                .AsNoTracking()
                .Where(u => u.ExternalId == userExternalId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var chatsFound = dbContext.Chats
                .AsNoTracking()
                .Where(chat => chat.UserId == userId)
                .OrderByDescending(c=>c.LastUpdated);
            return chatsFound;
        }

        public async Task<Chat?> GetChat(Guid chatId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var chatFound = await dbContext.Chats
                .AsNoTracking()
                .Include(chat=>chat.Messages)
                .FirstOrDefaultAsync(chat => chat.Id == chatId);
            return chatFound;
        }

        public async Task<ResponseResult> UpdateChat(Chat chat)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var chatFound = await dbContext.Chats
                .FirstOrDefaultAsync(c => c.Id == chat.Id);
            if (chatFound == null)
            {
                return new ResponseResult(false,
                    $"Chat with Id = {chat.Id} not found");
            }
            chatFound.Title = chat.Title;
            chatFound.LastUpdated = DateTime.Now;
            dbContext.Chats.Update(chatFound);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true, 
                $"Chat with Id = {chat.Id} updated");
        }

        public async Task<ResponseResult> DeleteChat(Guid chatId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var chatFound = await dbContext.Chats
                .FirstOrDefaultAsync(c => c.Id == chatId);
            if (chatFound == null)
            {
                return new ResponseResult(false,
                    $"Chat with Id = {chatId} not found");
            }

            dbContext.Chats.Remove(chatFound);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true,
                $"Chat with Id = {chatId} deleted");
        }
    }
}
