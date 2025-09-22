using System.ComponentModel.DataAnnotations;

namespace CodeExplainerCommon.DTOs
{
    public class ChatReadDTO
    {
        [Key]
        public Guid Id { get; set; }
        public string Title { get; set; }
        public IEnumerable<MessageReadDTO> Messages { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
