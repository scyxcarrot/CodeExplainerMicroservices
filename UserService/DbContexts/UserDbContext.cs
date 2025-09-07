using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using UserService.Constants;
using UserService.Models;

namespace UserService.DbContexts
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) :
        IdentityDbContext<AppUser>(options)
    {
        public DbSet<UserToken> UserTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            var identityRoles = new List<IdentityRole>()
            {
                new IdentityRole
                {
                    Id = "A1A1A1A1-1A1A-1A1A-1A1A-1A1A1d1A1A1A", 
                    Name = Role.Admin, 
                    NormalizedName = Role.Admin.ToUpperInvariant()
                },
                new IdentityRole
                {
                    Id = "B2B2B2B2-2B2B-2B2B-2B2B-2B2B2d2B2B2B", 
                    Name = Role.User, 
                    NormalizedName = Role.User.ToUpperInvariant()
                },
            };

            builder.Entity<IdentityRole>().HasData(identityRoles);
        }
    }
}
