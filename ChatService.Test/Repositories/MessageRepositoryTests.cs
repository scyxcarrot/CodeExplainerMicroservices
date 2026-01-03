using ChatService.DbContexts;
using ChatService.Models;
using ChatService.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ChatService.Test.Repositories
{
    public class MessageRepositoryTests : IAsyncLifetime
    {
        private const string DatabaseName = "ChatDbTest";
        private readonly IDbContextFactory<ChatDbContext> _dbContextFactory;

        private AppUser _appUser;

        // SUT
        private readonly IMessageRepository _messageRepository;

        public MessageRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(DatabaseName)
                .Options;

            // Mock the Factory
            var mockFactory = new Mock<IDbContextFactory<ChatDbContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
                .ReturnsAsync(() => new ChatDbContext(options));

            _dbContextFactory = mockFactory.Object;
            _messageRepository = new MessageRepository(_dbContextFactory);
        }

        public async Task InitializeAsync()
        {
            await SeedDatabase();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private async Task SeedDatabase()
        {
            await using var context = new ChatDbContext(
                new DbContextOptionsBuilder<ChatDbContext>()
                    .UseInMemoryDatabase(DatabaseName)
                    .Options);

            _appUser = new AppUser
            {
                Id = Guid.CreateVersion7(),
                ExternalId = Guid.CreateVersion7().ToString()
            };
            await context.AppUsers.AddAsync(_appUser);

            Chat chat = new Chat()
            {
                LastUpdated = DateTime.Now,
                Messages = new List<Message>(),
                Title = "ChatTitle",
                User = _appUser,
                UserId = _appUser.Id,
            };

            for (int index = 0; index < 3; index++)
            {
                Message message = new Message()
                {
                    Chat = chat,
                    ChatId = chat.Id,
                    ChatRole = "user",
                    MessageOrder = index,
                    TextMessage = $"TextMessage{index}",
                    TimeStamp = DateTime.Now,
                };

                await context.Messages.AddAsync(message);
            }
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateMessage_ReturnsSuccess()
        {
            // Arrange
            Message message = new Message()
            {
                ChatRole = "user",
                MessageOrder = 3,
                TextMessage = "TextMessageSample",
            };

            // Act
            var responseResult = await _messageRepository.CreateMessage(message);

            // Assert
            responseResult.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetMessage_ReturnsNullIfNotExist()
        {
            // Arrange
            // Act
            var messageReadDTO = await _messageRepository.GetMessageById(Guid.CreateVersion7());

            // Assert
            messageReadDTO.Should().BeNull();
        }

        [Fact]
        public async Task GetMessage_ReturnsOk()
        {
            // Arrange
            // Arrange
            Message message = new Message()
            {
                ChatRole = "user",
                MessageOrder = 3,
                TextMessage = "TextMessageSample",
            };
            await _messageRepository.CreateMessage(message);

            // Act
            var messageReadDTO = await _messageRepository.GetMessageById(message.Id);

            // Assert
            messageReadDTO.Should().NotBeNull();
            messageReadDTO.TextMessage.Should().Be(message.TextMessage);
            messageReadDTO.MessageOrder.Should().Be(message.MessageOrder);
            messageReadDTO.ChatRole.Should().Be(message.ChatRole);
        }

        [Fact]
        public async Task UpdateMessage_ReturnsSuccess()
        {
            // Arrange
            // Arrange
            Message message = new Message()
            {
                ChatRole = "user",
                MessageOrder = 3,
                TextMessage = "TextMessageSample",
            };
            await _messageRepository.CreateMessage(message);

            // Act
            message.TextMessage = "UpdatedTextMessage";
            var responseResult = await _messageRepository.UpdateMessage(message);

            // Assert
            responseResult.Success.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteMessage_ReturnsSuccess()
        {
            // Arrange
            Message message = new Message()
            {
                ChatRole = "user",
                MessageOrder = 3,
                TextMessage = "TextMessageSample",
            };
            await _messageRepository.CreateMessage(message);

            // Act
            var responseResult = await _messageRepository.DeleteMessage(message.Id);

            // Assert
            responseResult.Success.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteMessage_ReturnsFailIfNotExist()
        {
            // Arrange
            // Act
            var responseResult = await _messageRepository.DeleteMessage(Guid.CreateVersion7());

            // Assert
            responseResult.Success.Should().BeFalse();
        }
    }
}
