using ChatService.DbContexts;
using ChatService.Models;
using ChatService.Repositories;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Moq;

namespace ChatService.Test.Repositories
{
    public class ChatRepositoryTests : IAsyncLifetime
    {
        private const string DatabaseName = "ChatDbTest";
        private readonly IDbContextFactory<ChatDbContext> _dbContextFactory;

        private AppUser _appUser;

        // SUT
        private IChatRepository _chatRepository;

        public ChatRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseInMemoryDatabase(DatabaseName)
                .Options;

            // Mock the Factory
            var mockFactory = new Mock<IDbContextFactory<ChatDbContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
                .ReturnsAsync(() => new ChatDbContext(options));

            _dbContextFactory = mockFactory.Object;
            _chatRepository = new ChatRepository(_dbContextFactory);
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

            for (int index = 0; index < 3; index++)
            {
                Chat chat = new Chat()
                {
                    LastUpdated = DateTime.Now,
                    Messages = new List<Message>(),
                    Title = $"Title{index}",
                    User = _appUser,
                    UserId = _appUser.Id,
                };

                await context.Chats.AddAsync(chat);
            }
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateChat_ReturnsSuccess()
        {
            // Arrange
            Chat chat = new Chat()
            {
                LastUpdated = DateTime.Now,
                Messages = new List<Message>(),
                Title = "SampleTitle",
            };

            // Act
            var responseResult = await _chatRepository.CreateChat(chat, _appUser.ExternalId);

            // Assert
            responseResult.Success.Should().BeTrue();
        }

        [Fact]
        public async Task CreateChat_ReturnsFailIfExternalIdNotPresent()
        {
            // Arrange
            Chat chat = new Chat()
            {
                LastUpdated = DateTime.Now,
                Messages = new List<Message>(),
                Title = "SampleTitle",
            };

            // Act
            var responseResult = await _chatRepository.CreateChat(chat, Guid.CreateVersion7().ToString());

            // Assert
            responseResult.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetChat_ReturnsNullIfNotExist()
        {
            // Arrange
            // Act
            var chatReadDTO = await _chatRepository.GetChat(Guid.CreateVersion7());

            // Assert
            chatReadDTO.Should().BeNull();
        }

        [Fact]
        public async Task GetChat_ReturnsOk()
        {
            // Arrange
            // Arrange
            Chat chat = new Chat()
            {
                LastUpdated = DateTime.Now,
                Messages = new List<Message>(),
                Title = "SampleTitle",
            };
            await _chatRepository.CreateChat(chat, _appUser.ExternalId);

            // Act
            var chatReadDTO = await _chatRepository.GetChat(chat.Id);

            // Assert
            chatReadDTO.Should().NotBeNull();
            chatReadDTO.Title.Should().Be(chat.Title);
        }

        [Fact]
        public async Task UpdateChat_ReturnsSuccess()
        {
            // Arrange
            // Arrange
            Chat chat = new Chat()
            {
                LastUpdated = DateTime.Now,
                Messages = new List<Message>(),
                Title = "SampleTitle",
            };
            await _chatRepository.CreateChat(chat, _appUser.ExternalId);

            // Act
            chat.Title = "UpdatedTitle";
            var responseResult = await _chatRepository.UpdateChat(chat);

            // Assert
            responseResult.Success.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteChat_ReturnsSuccess()
        {
            // Arrange
            Chat chat = new Chat()
            {
                LastUpdated = DateTime.Now,
                Messages = new List<Message>(),
                Title = "SampleTitle",
            };
            await _chatRepository.CreateChat(chat, _appUser.ExternalId);

            // Act
            var responseResult = await _chatRepository.DeleteChat(chat.Id);

            // Assert
            responseResult.Success.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteChat_ReturnsFailIfNotExist()
        {
            // Arrange
            // Act
            var responseResult = await _chatRepository.DeleteChat(Guid.CreateVersion7());

            // Assert
            responseResult.Success.Should().BeFalse();
        }
    }
}
