using System.ComponentModel.DataAnnotations;

namespace ChatService.Models
{
    public class Chat
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public AppUser User { get; set; }

        [Required]
        public string Title { get; set; }

        public List<Message> Messages { get; set; } = new List<Message>();

        [Required]
        public DateTime LastUpdated { get; set; }
    }
}
