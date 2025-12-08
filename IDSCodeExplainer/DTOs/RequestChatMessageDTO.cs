namespace IDSCodeExplainer.DTOs
{
    public class RequestChatMessageDTO
    {
        public Guid UserId { get; set; }
        public Guid ChatId { get; set; }
        public string Message { get; set; }
    }
}
