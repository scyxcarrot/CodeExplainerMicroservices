using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatService.DbContexts
{
    public class ChatDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
    {
        public ChatDbContext CreateDbContext(string[] args)
        {
            var dbContextOptionBuilder = new DbContextOptionsBuilder<ChatDbContext>()
                .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=ChatDb;")
                .Options;

            return new ChatDbContext(dbContextOptionBuilder);
        }
    }
}
