using Microsoft.EntityFrameworkCore;

namespace ChatService.DbContexts
{
    public class ChatDbContextFactory(string connectionString) : IDbContextFactory<ChatDbContext>
    {
        public ChatDbContext CreateDbContext()
        {
            var dbContextOptionBuilder = new DbContextOptionsBuilder<ChatDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new ChatDbContext(dbContextOptionBuilder);
        }
    }
}
