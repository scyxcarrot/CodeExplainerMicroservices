using System.Reflection;

using CodeExplainerCommon.Contracts;
using CodeExplainerCommon.Responses;

using FluentAssertions;

using MassTransit;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Moq;

using UserService.Constants;
using UserService.DbContexts;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Test.Repositories
{
    public class UserRepositoryTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<SignInManager<AppUser>> _mockSignInManager;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly UserDbContext _userDbContext;
        private readonly UserRepository _userRepository;
        public UserRepositoryTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(
                store.Object, 
                null, 
                null, 
                null, 
                null, 
                null, 
                null, 
                null, 
                null);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
            _mockSignInManager = new Mock<SignInManager<AppUser>>(
                _mockUserManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null, 
                null, 
                null, 
                null);
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();

            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _userDbContext = new UserDbContext(options); ;

            _userRepository = new UserRepository(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockPublishEndpoint.Object,
                _userDbContext
            );
        }

        [Fact]
        public async Task Login_ReturnsFalseIfUserDoesNotExist()
        {
            // Arrange
            _mockUserManager.Setup(userManager => userManager.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser)null);

            // Act
            var result = await _userRepository.Login("sample@email.com", "password");

            // Assert
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Login_ReturnsFalseIfPasswordFailed()
        {
            // Arrange
            _mockUserManager.Setup(userManager => userManager.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new AppUser());
            _mockSignInManager.Setup(signInManager=> signInManager.CheckPasswordSignInAsync(
                It.IsAny<AppUser>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
            )).ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _userRepository.Login("sample@email.com", "password");

            // Assert
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Login_ReturnsTrueIfUserAndPasswordSuccess()
        {
            // Arrange
            _mockUserManager.Setup(userManager => userManager.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new AppUser());
            _mockSignInManager.Setup(signInManager => signInManager.CheckPasswordSignInAsync(
                It.IsAny<AppUser>(),
                It.IsAny<string>(),
                It.IsAny<bool>()
            )).ReturnsAsync(SignInResult.Success);

            // Act
            var result = await _userRepository.Login("sample@email.com", "password");

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ChangePassword_ReturnsFalseIfUserDoesNotExist()
        {
            // Arrange
            _mockUserManager.Setup(userManager => userManager.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser)null);

            // Act
            var result = await _userRepository.ChangePassword(
                "SampleUserId", 
                "sample@email.com", 
                "password");

            // Assert
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ChangePassword_ReturnsFalseIfChangeFailed()
        {
            // Arrange
            _mockUserManager.Setup(userManager => userManager.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new AppUser());
            _mockUserManager
                .Setup(userManager => userManager.ChangePasswordAsync(
                    It.IsAny<AppUser>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed());

            // Act
            var result = await _userRepository.ChangePassword(
                "SampleUserId",
                "sample@email.com",
                "password");

            // Assert
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ChangePassword_ReturnsTrueIfChangeSuccess()
        {
            // Arrange
            _mockUserManager.Setup(userManager => userManager.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new AppUser());
            _mockUserManager
                .Setup(userManager => userManager.ChangePasswordAsync(
                    It.IsAny<AppUser>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userRepository.ChangePassword(
                "SampleUserId",
                "sample@email.com",
                "password");

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task RegisterUser_ShouldCommitTransaction_WhenAllStepsSucceed()
        {
            // Arrange
            var user = new AppUser { Email = "new@test.com", Id = "1" };
            var roles = new List<string> { "Admin" };

            _mockUserManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync((AppUser)null);
            _mockUserManager.Setup(x => x.CreateAsync(user, "Pass123!")).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRolesAsync(user, roles)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userRepository.RegisterUser(user, "Pass123!", roles);

            // Assert
            result.Success.Should().BeTrue();
            _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<UserCreated>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task RegisterUser_ShouldFailIfRoleNotExist()
        {
            // Arrange
            var user = new AppUser { Email = "new@test.com", Id = "1" };
            var roles = new List<string> { "InvalidRole" };

            // Act
            var result = await _userRepository.RegisterUser(user, "Pass123!", roles);

            // Assert
            result.Success.Should().BeFalse();
            _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<UserCreated>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        public async Task GetAllRoles_ShouldReturnAllRoles()
        {
            // Arrange
            var roleFields = typeof(Role).GetFields(
                    BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            var allRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            allRoles = roleFields
                .Select(roleField => (string)roleField.GetValue(null))
                .ToHashSet();

            // Act
            var result = _userRepository.GetAllRoles();

            // Assert
            result.Should().BeEquivalentTo(allRoles);
        }

        [Fact]
        public async Task UpdateUser_ShouldUpdateDetailsAndRoles_WhenUserExistsAndRolesAreValid()
        {
            // Arrange
            var userId = "user-123";
            var existingUser = new AppUser { Id = userId, Email = "old@test.com", UserName = "oldUser" };
            var updatedData = new AppUser { Email = "new@test.com", UserName = "newUser" };
            var newRoles = new List<string> { "Admin" };
            var oldRoles = new List<string> { "User" };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockUserManager.Setup(um => um.UpdateAsync(existingUser))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(existingUser))
                .ReturnsAsync(oldRoles);
            _mockUserManager.Setup(um => um.RemoveFromRolesAsync(existingUser, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRolesAsync(existingUser, newRoles))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userRepository.UpdateUser(userId, updatedData, newRoles);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain(userId);
            existingUser.Email.Should().Be("new@test.com");
            existingUser.UserName.Should().Be("newUser");
            _mockUserManager.Verify(um => um.RemoveFromRolesAsync(existingUser, oldRoles), Times.Once);
            _mockUserManager.Verify(um => um.AddToRolesAsync(existingUser, newRoles), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ReturnsFalse_WhenRoleIsInvalid()
        {
            // Arrange
            var user = new AppUser { Id = "123" };
            _mockUserManager.Setup(um => um.FindByIdAsync("123")).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userRepository.UpdateUser("123", user, new List<string> { "InvalidRole" });

            // Assert
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateUser_ReturnsFalse_WhenIdentityUpdateFails()
        {
            // Arrange
            var user = new AppUser { Id = "123" };
            _mockUserManager.Setup(um => um.FindByIdAsync("123")).ReturnsAsync(user);
            var identityError = IdentityResult.Failed();
            _mockUserManager.Setup(um => um.UpdateAsync(user)).ReturnsAsync(identityError);

            // Act
            var result = await _userRepository.UpdateUser("123", user, new List<string>());

            // Assert
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateUser_ReturnsFalse_WhenUserDoesNotExist()
        {
            // Arrange
            _mockUserManager.Setup(um => um.FindByIdAsync("invalid-id"))
                .ReturnsAsync((AppUser)null);

            // Act
            var result = await _userRepository.UpdateUser("invalid-id", new AppUser(), new List<string>());

            // Assert
            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnSuccess_AndPublishEvent()
        {
            // Arrange
            var userId = "user-to-delete";
            var user = new AppUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userRepository.DeleteUser(userId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("deleted");
            _mockPublishEndpoint.Verify(p => p.Publish(
                    It.Is<UserDeleted>(e => e.Id == userId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnFalse_WhenUserDoesNotExist()
        {
            // Arrange
            _mockUserManager.Setup(um => um.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((AppUser)null);

            // Act
            var result = await _userRepository.DeleteUser("fake-id");

            // Assert
            result.Success.Should().BeFalse();
            _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<UserDeleted>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUser_ShouldReturnFalse_WhenDeleteAsyncFails()
        {
            // Arrange
            var user = new AppUser { Id = "123" };
            _mockUserManager.Setup(um => um.FindByIdAsync("123")).ReturnsAsync(user);

            var identityError = IdentityResult.Failed(new IdentityError { Description = "Cannot delete admin" });
            _mockUserManager.Setup(um => um.DeleteAsync(user)).ReturnsAsync(identityError);

            // Act
            var result = await _userRepository.DeleteUser("123");

            // Assert
            result.Success.Should().BeFalse();
            _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<UserDeleted>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
