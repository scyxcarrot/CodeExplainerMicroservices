using ChatService.Models;
using CodeExplainerCommon.Responses;

namespace ChatService.Repositories
{
    public interface IMessageRepository
    {
        public Task<ResponseResult> CreateMessage(Message message);
        public Task<Message?> GetMessageById(Guid messageId);
        public Task<ResponseResult> UpdateMessage(Message message);
        public Task<ResponseResult> DeleteMessage(Guid messageId);
    }
}
