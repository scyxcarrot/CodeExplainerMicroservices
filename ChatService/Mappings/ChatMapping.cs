using ChatService.Models;

namespace ChatService.Mappings
{
    public static class ChatMapping
    {
        public static Chat ToModel(this ChatCreateDTO chatCreateDTO)
        {
            return new Chat()
            {
                Title = chatCreateDTO.Title,
                LastUpdated = DateTime.UtcNow,
            };
        }

        public static Chat ToModel(this ChatUpdateDTO chatUpdateDTO, Guid chatId)
        {
            return new Chat()
            {
                Id = chatId,
                Title = chatUpdateDTO.Title,
                LastUpdated = DateTime.UtcNow,
            };
        }

        public static ChatReadDTO ToReadDTO(this Chat chat)
        {
            return new ChatReadDTO()
            {
                Id = chat.Id,
                Messages = chat.Messages.Select(message => new MessageReadDTO
                {
                    Id = message.Id,
                    ChatId = message.ChatId,
                    TimeStamp = message.TimeStamp,
                    TextMessage = message.TextMessage,
                    ChatRole = message.ChatRole,
                    MessageOrder = message.MessageOrder,
                }),
                Title = chat.Title,
                LastUpdated = chat.LastUpdated,
            };
        }
    }
}
