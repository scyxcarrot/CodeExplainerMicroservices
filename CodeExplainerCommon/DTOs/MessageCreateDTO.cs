namespace CodeExplainerCommon.DTOs
{
    public class MessageCreateDTO
    {
        public Guid ChatId { get; set; }
        public string ChatRole { get; set; }
        public string TextMessage { get; set; }
        public long MessageOrder { get; set; }
    }
}
