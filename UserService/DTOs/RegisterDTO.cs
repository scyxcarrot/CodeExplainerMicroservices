using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs
{
    public class RegisterDTO
    {
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public IEnumerable<string> Roles { get; set; }
    }
}
