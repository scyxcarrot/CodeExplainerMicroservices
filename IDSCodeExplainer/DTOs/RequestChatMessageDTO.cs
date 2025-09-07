namespace IDSCodeExplainer.DTOs
{
    public class RequestChatMessageDTO
    {
        public Guid UserId { get; set; }
        public Guid ChatId { get; set; }
        public List<ChatMessageDTO> ChatMessages { get; set; }
    }
}
