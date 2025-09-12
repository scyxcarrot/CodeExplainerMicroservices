namespace ChatService.Models
{
    public class MessageReadDTO
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }
        public string ChatRole { get; set; }
        public string TextMessage { get; set; }
        public DateTime TimeStamp { get; set; }
        public long MessageOrder { get; set; }
    }
}
