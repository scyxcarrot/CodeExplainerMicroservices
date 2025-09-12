using ChatService.Models;
using CodeExplainerCommon.Responses;

namespace ChatService.Repositories
{
    public interface IChatRepository
    {
        public Task<ResponseResult> CreateChat(Chat chat);
        public Task<IEnumerable<Chat>> GetAllChatsByUserExternalId(string userExternalId);
        public Task<Chat?> GetChat(Guid chatId);
        public Task<ResponseResult> UpdateChat(Chat chat);
        public Task<ResponseResult> DeleteChat(Guid chatId);
    }
}
