using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatService.Models;

namespace ChatService.Configurations
{
    public class ChatConfiguration : IEntityTypeConfiguration<Chat>
    {
        public void Configure(EntityTypeBuilder<Chat> builder)
        {
            builder.HasKey(c => c.Id);

            builder
                .HasOne(c => c.User)
                .WithMany(u => u.Chats)
                .HasForeignKey(c => c.UserId)
                .IsRequired();

            builder
                .HasMany(c => c.Messages)
                .WithOne(m=> m.Chat)
                .HasForeignKey(m => m.ChatId)
                .IsRequired();

            builder
                .HasIndex(c => c.LastUpdated)
                .IsUnique(false);
        }
    }
}
