namespace UserService.Models
{
    public class UserToken
    {
        public Guid Id { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryDate { get; set; }
        public bool Invalidated { get; set; }
        public string UserId { get; set; }
        public AppUser User { get; set; }
    }
}
