using System.Reflection;

using CodeExplainerCommon.Responses;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

using Moq;

using UserService.Constants;
using UserService.Controllers;
using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;
using UserService.Service;

namespace UserService.Test.Controller
{
    public class UserControllerTests
    {
        // SUT
        private readonly UserController _userController;

        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ITokenService> _mockTokenService;
        public UserControllerTests()
        {
            _mockTokenService = new Mock<ITokenService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _userController = new UserController(_mockTokenService.Object, _mockUserRepository.Object);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnNotFoundIfUserDoesNotExist()
        {
            // Arrange
            _mockUserRepository.Setup(u => u.GetUserById(It.IsAny<string>())).ReturnsAsync((AppUser)null);
            
            // Act
            var result = await _userController.GetUserById("abc");

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
            _mockUserRepository.Verify(repo => repo.GetUserById(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnUserReadDTO()
        {
            // Arrange
            string id = Guid.NewGuid().ToString();
            string username = "username";
            string email = "email";
            IEnumerable<string> roles = new List<string>() { "Admin", "User" };

            var appUser = new AppUser()
            {
                Id = id,
                UserName = username,
                Email = email,
            };
            _mockUserRepository.Setup(u => u.GetUserById(id)).ReturnsAsync(appUser);
            _mockUserRepository.Setup(u => u.GetRoles(appUser)).ReturnsAsync(roles);

            // Act
            var result = await _userController.GetUserById(id);

            // Assert
            var okObjectResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var userReadDTO = okObjectResult.Value.Should().BeOfType<UserReadDTO>().Subject;

            userReadDTO.Id.Should().Be(id);
            userReadDTO.UserName.Should().Be(username);
            userReadDTO.Email.Should().Be(email);
            userReadDTO.Roles.Should().Contain(roles);
        }

        [Fact]
        public async Task Register_ShouldReturnStatus500IfRegisterFailed()
        {
            // Arrange
            RegisterDTO registerDTO = new RegisterDTO();
            _mockUserRepository.Setup(userRepository => userRepository.RegisterUser(
                    It.IsAny<AppUser>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new ResponseResult(false, ""));

            // Act
            var result = await _userController.Register(registerDTO);

            // Assert
            ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Register_ShouldReturnCreatedAtRouteIfSuccess()
        {
            // Arrange
            RegisterDTO registerDTO = new RegisterDTO()
            {
                Email = "sample@email.com",
                Username = "sampleUsername",
                Roles = new List<string>() {"Admin"},
            };
            _mockUserRepository.Setup(userRepository => userRepository.RegisterUser(
                    It.IsAny<AppUser>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new ResponseResult(true, ""));

            // Act
            var result = await _userController.Register(registerDTO);

            // Assert
            CreatedAtRouteResult createdAtRouteResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            createdAtRouteResult.RouteName.Should().Be(nameof(_userController.GetUserById));
            UserReadDTO userReadDTO = createdAtRouteResult.Value.Should().BeOfType<UserReadDTO>().Subject;
            userReadDTO.Email.Should().Be(registerDTO.Email);
            userReadDTO.UserName.Should().Be(registerDTO.Username);
            userReadDTO.Roles.Should().Contain(registerDTO.Roles);
        }

        [Fact]
        public void GetAllRoles_ShouldReturnAllRoles()
        {
            // Arrange
            var roleFields = typeof(Role).GetFields(
                    BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            var allRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            allRoles = roleFields
                .Select(roleField => (string)roleField.GetValue(null))
                .ToHashSet();

            _mockUserRepository.Setup(userRepository => userRepository.GetAllRoles()).Returns(allRoles);
            // Act
            var result = _userController.GetAllRoles();

            // Assert
            OkObjectResult okObjectResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            IEnumerable<string> roles = okObjectResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject;
            roles.Should().Contain(allRoles);
        }

        [Fact]
        public async Task ChangePassword_ShouldReturnStatus500IfFail()
        {
            // Arrange
            string userId = Guid.CreateVersion7().ToString();
            ChangePasswordDTO changePasswordDTO = new ChangePasswordDTO()
            {
                OldPassword = "oldPassword",
                NewPassword = "newPassword"
            };
            _mockUserRepository
                .Setup(userRepository => userRepository.ChangePassword(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ResponseResult(false, ""));

            // Act
            var result = await _userController.ChangeUserPassword(userId, changePasswordDTO);

            // Assert
            ObjectResult objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ChangePassword_ShouldReturnNoContentIfSuccess()
        {
            // Arrange
            string userId = Guid.CreateVersion7().ToString();
            ChangePasswordDTO changePasswordDTO = new ChangePasswordDTO()
            {
                OldPassword = "oldPassword",
                NewPassword = "newPassword"
            };
            _mockUserRepository
                .Setup(userRepository => userRepository.ChangePassword(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ResponseResult(true, ""));

            // Act
            var result = await _userController.ChangeUserPassword(userId, changePasswordDTO);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnUsers()
        {
            // Arrange
            IEnumerable<AppUser> appUsers = new List<AppUser>()
            {
                new() {UserName = "SampleUsername", Email = "Sample@Email.com"}
            };
            _mockUserRepository
                .Setup(userRepository => userRepository.GetAllUsers())
                .ReturnsAsync(appUsers);
            _mockUserRepository
                .Setup(userRepository => userRepository.GetRoles(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string>(){"user"});

            // Act
            var result = await _userController.GetAllUsers();

            // Assert
            OkObjectResult okObjectResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            IEnumerable<UserReadDTO> userReadDTOs = okObjectResult.Value.Should().BeAssignableTo<IEnumerable<UserReadDTO>>().Subject;
            userReadDTOs.Count().Should().Be(appUsers.Count());
        }

        [Fact]
        public async Task Delete_ReturnNotFound()
        {
            string userId = Guid.CreateVersion7().ToString();

            _mockUserRepository.Setup(userRepository => userRepository.DeleteUser(userId))
                .ReturnsAsync(new ResponseResult(false, ""));

            // Act
            var result = await _userController.DeleteUser(userId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
            _mockUserRepository.Verify(userRepository => userRepository.DeleteUser(userId), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnNoContent()
        {
            string userId = Guid.CreateVersion7().ToString();

            _mockUserRepository.Setup(userRepository => userRepository.DeleteUser(userId))
                .ReturnsAsync(new ResponseResult(true, ""));

            // Act
            var result = await _userController.DeleteUser(userId);

            // Assert
            result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
            _mockUserRepository.Verify(userRepository => userRepository.DeleteUser(userId), Times.Once);
        }
    }
}