using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserService.DbContexts
{
    public class UserDesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
    {
        public UserDbContext CreateDbContext(string[] args)
        {
            var dbContextOptionBuilder = new DbContextOptionsBuilder<UserDbContext>()
                .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=UserDb;")
                .Options;

            return new UserDbContext(dbContextOptionBuilder);
        }
    }
}
