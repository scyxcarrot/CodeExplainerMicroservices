using System.ComponentModel.DataAnnotations;

namespace ChatService.Models
{
    public class Message
    {
        public Guid Id { get; set; }

        [Required]
        public Guid ChatId { get; set; }
        public Chat Chat { get; set; }

        [Required]
        public string ChatRole { get; set; }

        [Required]
        public string TextMessage { get; set; }

        [Required]
        public DateTime TimeStamp { get; set; }

        [Required]
        public long MessageOrder { get; set; }
    }
}
