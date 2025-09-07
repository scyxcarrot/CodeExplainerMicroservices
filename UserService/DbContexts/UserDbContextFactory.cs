using Microsoft.EntityFrameworkCore;

namespace UserService.DbContexts
{
    public class UserDbContextFactory(string connectionString) : IDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext()
        {
            var dbContextOptionBuilder = new DbContextOptionsBuilder<UserDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new UserDbContext(dbContextOptionBuilder);
        }
    }
}
