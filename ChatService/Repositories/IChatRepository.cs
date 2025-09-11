using ChatService.Models;
using CodeExplainerCommon.Responses;

namespace ChatService.Repositories
{
    public interface IChatRepository
    {
        public Task<ResponseResult> CreateChat(Chat chat);
        public Task<IEnumerable<Chat>> GetAllChatsByUserId(Guid userId);
        public Task<Chat?> GetChat(Guid chatId);
        public Task<ResponseResult> UpdateChat(Chat chat);
        public Task<ResponseResult> DeleteChat(Guid chatId);
    }
}
