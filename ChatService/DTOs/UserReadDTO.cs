namespace ChatService.DTOs
{
    public class UserReadDTO
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; }
        public IEnumerable<Guid> ChatIds { get; set; } = new List<Guid>();
    }
}
