using ChatService.Models;

using CodeExplainerCommon.DTOs;

using static MassTransit.Monitoring.Performance.BuiltInCounters;

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
            IEnumerable<MessageReadDTO> messageReadDTOs = new List<MessageReadDTO>();
            if (chat.Messages.Any())
            {
                messageReadDTOs = chat.Messages.Select(message => new MessageReadDTO
                {
                    Id = message.Id,
                    ChatId = message.ChatId,
                    TimeStamp = message.TimeStamp,
                    TextMessage = message.TextMessage,
                    ChatRole = message.ChatRole,
                    MessageOrder = message.MessageOrder,
                });
            }

            return new ChatReadDTO()
            {
                Id = chat.Id,
                Title = chat.Title,
                LastUpdated = chat.LastUpdated,
                Messages = messageReadDTOs,
            };
        }
    }
}
