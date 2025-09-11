using Microsoft.EntityFrameworkCore;

using ChatService.Models;

namespace ChatService.DbContexts
{
    public class ChatDbContext(DbContextOptions<ChatDbContext> options) :
        DbContext(options)
    {
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
