namespace UserService.DTOs
{
    public class UpdateUserDTO
    {
        public string Email { get; set; }
        public string Username { get; set; }

        public IEnumerable<string> Roles { get; set; }
    }
}
