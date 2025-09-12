using ChatService.Models;

namespace ChatService.Mappings
{
    public static class MessageMapping
    {
        public static Message ToModel(this MessageCreateDTO messageCreateDTO)
        {
            return new Message()
            {
                ChatId = messageCreateDTO.ChatId,
                ChatRole = messageCreateDTO.ChatRole,
                TextMessage = messageCreateDTO.TextMessage,
            };
        }

        public static Message ToModel(this MessageUpdateDTO messageUpdateDTO, Guid messageId)
        {
            return new Message()
            {
                ChatId = messageUpdateDTO.ChatId,
                ChatRole = messageUpdateDTO.ChatRole,
                TextMessage = messageUpdateDTO.TextMessage,
            };
        }

        public static MessageReadDTO ToReadDTO(this Message message)
        {
            return new MessageReadDTO()
            {
                Id = message.Id,
                ChatId = message.ChatId,
                ChatRole = message.ChatRole,
                MessageOrder = message.MessageOrder,
                TextMessage = message.TextMessage,
                TimeStamp = message.TimeStamp,
            };
        }
    }
}
