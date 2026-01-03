using ChatService.DbContexts;
using ChatService.Models;

using CodeExplainerCommon.DTOs;
using CodeExplainerCommon.Responses;

using Microsoft.EntityFrameworkCore;

namespace ChatService.Repositories
{
    public class ChatRepository(IDbContextFactory<ChatDbContext> dbContextFactory) : IChatRepository
    {
        public async Task<ResponseResult> CreateChat(Chat chat, string externalUserId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var userFound = await dbContext.AppUsers.FirstAsync(user=>user.ExternalId ==  externalUserId);
            if (userFound == null) 
            { 
                return new ResponseResult(false, "User Id not found"); 
            }
            chat.UserId = userFound.Id;

            await dbContext.Chats.AddAsync(chat);
            await dbContext.SaveChangesAsync();
            return new ResponseResult(true,
                $"Chat {chat.Title} created");
        }

        public async Task<ChatReadDTO?> GetChat(Guid chatId)
        {
            var dbContext = await dbContextFactory.CreateDbContextAsync();
            var chatFound = await dbContext.Chats
                .AsNoTracking()
                .Where(chat => chat.Id == chatId)
                .Select(chat => new ChatReadDTO // Projecting to the DTO is the fix
                {
                    Id = chat.Id,
                    LastUpdated = chat.LastUpdated,
                    Title = chat.Title,
                    Messages = chat.Messages.OrderBy(m => m.MessageOrder)
                    .Select(message => new MessageReadDTO {
                        Id = message.Id,
                        ChatId = message.ChatId,
                        TimeStamp = message.TimeStamp,
                        TextMessage = message.TextMessage,
                        ChatRole = message.ChatRole,
                        MessageOrder = message.MessageOrder,
                    }).ToList()
                })
                .FirstOrDefaultAsync();

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
