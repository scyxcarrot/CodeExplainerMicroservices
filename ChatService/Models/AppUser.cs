using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatService.Models
{
    public class AppUser
    {
        // this Id is the same as the id generated from User service
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string ExternalId { get; set; }

        public IEnumerable<Chat> Chats { get; set; }
    }
}
