using ChatService.Models;
using CodeExplainerCommon.Responses;

namespace ChatService.Repositories
{
    public interface IMessageRepository
    {
        public Task<ResponseResult> CreateMessage(Message message);
        public Task<IEnumerable<Message>> GetAllMessagesByChatId(Guid chatId);
        public Task<ResponseResult> UpdateMessage(Message message);
        public Task<ResponseResult> DeleteMessage(Guid messageId);
    }
}
