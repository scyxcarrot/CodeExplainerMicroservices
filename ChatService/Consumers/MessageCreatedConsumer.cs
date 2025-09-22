using System;
using ChatService.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Consumers
{
    public class MessageCreatedConsumer(IDbContextFactory<ChatDbContext> dbContextFactory)
    {
    }
}
