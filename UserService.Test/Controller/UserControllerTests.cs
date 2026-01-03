using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

using Moq;

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

        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ITokenService> _tokenServiceMock;
        public UserControllerTests()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _userController = new UserController(_tokenServiceMock.Object, _userRepositoryMock.Object);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnNotFoundIfUserDoesNotExist()
        {
            // Arrange
            _userRepositoryMock.Setup(u => u.GetUserById(It.IsAny<string>())).ReturnsAsync((AppUser)null);
            
            // Act
            var result = await _userController.GetUserById("abc");

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
            _userRepositoryMock.Verify(repo => repo.GetUserById(It.IsAny<string>()), Times.Once);
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
            _userRepositoryMock.Setup(u => u.GetUserById(id)).ReturnsAsync(appUser);
            _userRepositoryMock.Setup(u => u.GetRoles(appUser)).ReturnsAsync(roles);

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
    }
}