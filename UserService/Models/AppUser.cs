using Microsoft.AspNetCore.Identity;

namespace UserService.Models
{
    public class AppUser : IdentityUser
    {
        public List<UserToken> UserTokens { get; private set; }
    }
}
