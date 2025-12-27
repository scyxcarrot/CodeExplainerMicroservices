using Microsoft.EntityFrameworkCore;

using ChatService.Models;
using MassTransit;

namespace ChatService.DbContexts
{
    public class ChatDbContext(DbContextOptions<ChatDbContext> options) :
        DbContext(options)
    {
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddTransactionalOutboxEntities();
        }
    }
}
