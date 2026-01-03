using ChatService.Models;

using CodeExplainerCommon.DTOs;
using CodeExplainerCommon.Responses;

namespace ChatService.Repositories
{
    public interface IChatRepository
    {
        public Task<ResponseResult> CreateChat(Chat chat, string externalUserId);
        public Task<ChatReadDTO?> GetChat(Guid chatId);
        public Task<ResponseResult> UpdateChat(Chat chat);
        public Task<ResponseResult> DeleteChat(Guid chatId);
    }
}
