using System.ComponentModel.DataAnnotations;

namespace ChatService.DTOs
{
    public class UserCreateDTO
    {
        [Required]
        public string ExternalId { get; set; }
    }
}
