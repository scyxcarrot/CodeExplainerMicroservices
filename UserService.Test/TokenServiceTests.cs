using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Moq;

using UserService.DbContexts;
using UserService.Models;
using UserService.Service;

namespace UserService.Test
{
    public class TokenServiceTests
    {
        // SUT
        private readonly TokenService _tokenService;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly IDbContextFactory<UserDbContext> _dbContextFactory;

        public TokenServiceTests()
        {
            // SUT
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["JWT:SigningKey"]).Returns("super_secret_key_that_is_long_enough_for_sha256");
            _configMock.Setup(c => c["JWT:Issuer"]).Returns("test_issuer");
            _configMock.Setup(c => c["JWT:Audience"]).Returns("test_audience");

            // 2. Mock UserManager (Identity boilerplate)
            var store = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // 3. Setup InMemory Database for DBContextFactory
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var mockFactory = new Mock<IDbContextFactory<UserDbContext>>();
            mockFactory.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
                .ReturnsAsync(() => new UserDbContext(options));

            _dbContextFactory = mockFactory.Object;

            _tokenService = new TokenService(_configMock.Object, _userManagerMock.Object, _dbContextFactory);
        }

        [Fact]
        public async Task CreateToken_ShouldReturnValidString_WhenUserIsValid()
        {
            // Arrange
            var user = new AppUser { Id = "1", Email = "test@test.com", UserName = "testuser" };
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            // Act
            var token = await _tokenService.CreateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            // JWT format: header.payload.signature
            token.Split('.').Length.Should().Be(3);
        }

        [Fact]
        public async Task CreateRefreshToken_ShouldStoreTokenInDatabase()
        {
            // Arrange
            var user = new AppUser { Id = "user-123" };

            // Act
            var refreshToken = await _tokenService.CreateRefreshToken(user);

            // Assert
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var storedToken = await context.UserTokens.FirstOrDefaultAsync(t => t.UserId == "user-123");

            storedToken.Should().NotBeNull();
            storedToken!.RefreshToken.Should().Be(refreshToken);
            storedToken.Invalidated.Should().BeFalse();
        }

        [Fact]
        public async Task GetUserIdFromRefreshToken_ShouldReturnEmpty_WhenTokenIsExpired()
        {
            // Arrange
            var expiredToken = "expired-token";
            await using (var context = await _dbContextFactory.CreateDbContextAsync())
            {
                context.UserTokens.Add(new UserToken
                {
                    RefreshToken = expiredToken,
                    UserId = "user-1",
                    RefreshTokenExpiryDate = DateTime.UtcNow.AddHours(-1), // Past
                    Invalidated = false
                });
                await context.SaveChangesAsync();
            }

            // Act
            var userId = await _tokenService.GetUserIdFromRefreshToken(expiredToken);

            // Assert
            userId.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteRefreshToken_ShouldRemoveToken_WhenExists()
        {
            // Arrange
            var userId = "user-to-delete";
            await using (var context = await _dbContextFactory.CreateDbContextAsync())
            { 
                context.UserTokens.Add(new UserToken { UserId = userId, RefreshToken = "token-123" });
                await context.SaveChangesAsync();
            }

            // Act
            var result = await _tokenService.DeleteRefreshToken(userId);

            // Assert
            result.Should().BeTrue();
            using var checkContext = await _dbContextFactory.CreateDbContextAsync();
            checkContext.UserTokens.Any(t => t.UserId == userId).Should().BeFalse();
        }
    }
}
