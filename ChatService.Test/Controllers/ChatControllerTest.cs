using ChatService.Controllers;
using ChatService.Mappings;
using ChatService.Models;
using ChatService.Repositories;

using CodeExplainerCommon.DTOs;
using CodeExplainerCommon.Responses;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

using Moq;

namespace ChatService.Test.Controllers
{
    public class ChatControllerTest
    {
        private readonly Mock<IChatRepository> _chatRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;

        // SUT
        private readonly ChatController _chatController;

        public ChatControllerTest()
        {
            _chatRepositoryMock = new Mock<IChatRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();

            _chatController = new ChatController(_chatRepositoryMock.Object, _userRepositoryMock.Object);
        }

        [Fact]
        public async Task GetChatById_ReturnsNotFoundIfNull()
        {
            // Arrange
            _chatRepositoryMock
                .Setup(chatRepository => chatRepository.GetChat(It.IsAny<Guid>()))
                .ReturnsAsync((ChatReadDTO) null);

            // Act
            var result = await _chatController.GetChatById(Guid.CreateVersion7());

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
            _chatRepositoryMock.Verify(repo => repo.GetChat(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetChatById_ReturnsChatReadDTO()
        {
            // Arrange
            ChatReadDTO chatReadDTO = new ChatReadDTO()
            {
                Id = Guid.CreateVersion7(),
                LastUpdated = DateTime.Now,
                Messages = new List<MessageReadDTO>(),
                Title = "SampleChatTitle",
            };

            _chatRepositoryMock
                .Setup(chatRepository => chatRepository.GetChat(It.IsAny<Guid>()))
                .ReturnsAsync(chatReadDTO);
            // Act
            var result = await _chatController.GetChatById(Guid.CreateVersion7());

            // Assert
            _chatRepositoryMock.Verify(repo => repo.GetChat(It.IsAny<Guid>()), Times.Once);
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var resultChatReadDTO = okResult.Value.Should().BeOfType<ChatReadDTO>().Subject;

            resultChatReadDTO.Id.Should().Be(chatReadDTO.Id);
            resultChatReadDTO.LastUpdated.Should().Be(chatReadDTO.LastUpdated);
            resultChatReadDTO.Messages.Should().BeEmpty();
            resultChatReadDTO.Title.Should().Be(chatReadDTO.Title);
        }

        [Fact]
        public async Task GetAllChats_ShouldReturnValues()
        {
            // Arrange
            string userExternalId = Guid.CreateVersion7().ToString();
            Chat sampleChat = new Chat()
            {
                Id = Guid.CreateVersion7(),
                LastUpdated = DateTime.Now,
            };
            Message message = new Message()
            {
                Chat = sampleChat,
                ChatId = sampleChat.Id,
                ChatRole = "user",
                Id = Guid.CreateVersion7(),
                MessageOrder = 0,
                TextMessage = "SampleMessage",
                TimeStamp = DateTime.Now,
            };
            sampleChat.Messages = new List<Message>() { message };

            AppUser appUser = new AppUser()
            {
                Id = Guid.CreateVersion7(),
                ExternalId = userExternalId,
                Chats = new List<Chat>() { sampleChat },
            };

            _userRepositoryMock
                .Setup(userRepository => userRepository.GetOrCreateUserByExternalId(userExternalId))
                .ReturnsAsync(appUser);

            // Act
            var result = await _chatController.GetAllChats(userExternalId);

            // Assert
            OkObjectResult okObjectResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            IEnumerable<ChatReadDTO> resultChatReadDTO = okObjectResult.Value.Should()
                .BeAssignableTo<IEnumerable<ChatReadDTO>>().Subject;
            resultChatReadDTO.Should().BeEquivalentTo([sampleChat.ToReadDTO()]);
        }

        [Fact]
        public async Task Update_ReturnBadRequestIfUpdateFailed()
        {
            Guid chatId = Guid.CreateVersion7();
            string title = "ChatTitle";
            ChatUpdateDTO chatUpdateDTO = new ChatUpdateDTO() { Title = "title" };
            Chat chat = chatUpdateDTO.ToModel(chatId);

            _chatRepositoryMock.Setup(chatRepository => chatRepository.UpdateChat(It.IsAny<Chat>()))
                .ReturnsAsync(new ResponseResult(false, ""));
            
            // Act
            var result = await _chatController.Update(chatId, chatUpdateDTO);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
            _chatRepositoryMock.Verify(chatRepository => chatRepository.UpdateChat(It.IsAny<Chat>()), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnOkRequestIfUpdateSuccess()
        {
            Guid chatId = Guid.CreateVersion7();
            string title = "ChatTitle";
            ChatUpdateDTO chatUpdateDTO = new ChatUpdateDTO() { Title = "title" };
            Chat chat = chatUpdateDTO.ToModel(chatId);
            chat.Messages = new List<Message>();
            ChatReadDTO chatReadDTO = chat.ToReadDTO();

            _chatRepositoryMock.Setup(chatRepository => chatRepository.UpdateChat(It.IsAny<Chat>()))
                .ReturnsAsync(new ResponseResult(true, ""));
            _chatRepositoryMock.Setup(chatRepository => chatRepository.GetChat(chatId))
                .ReturnsAsync(chatReadDTO);

            // Act
            var result = await _chatController.Update(chatId, chatUpdateDTO);

            // Assert
            OkObjectResult okObjectResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            ChatReadDTO resultChatReadDTO = okObjectResult.Value.Should().BeOfType<ChatReadDTO>().Subject;
            resultChatReadDTO.Should().Be(chatReadDTO);
            _chatRepositoryMock.Verify(chatRepository => chatRepository.UpdateChat(It.IsAny<Chat>()), Times.Once);
        }

        [Fact]
        public async Task Create_ReturnStatusCode500IfNotCreated()
        {
            string title = "ChatTitle";
            ChatCreateDTO chatCreateDTO = new ChatCreateDTO()
            {
                Title = title,
                UserId = Guid.CreateVersion7().ToString(),
            };

            _chatRepositoryMock.Setup(chatRepository => chatRepository.CreateChat(It.IsAny<Chat>(), chatCreateDTO.UserId))
                .ReturnsAsync(new ResponseResult(false, ""));

            // Act
            var result = await _chatController.Create(chatCreateDTO);

            // Assert
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
            _chatRepositoryMock.Verify(chatRepository => chatRepository.CreateChat(It.IsAny<Chat>(), chatCreateDTO.UserId), Times.Once);
        }

        [Fact]
        public async Task Create_ReturnCreatedAtRoute()
        {
            string title = "ChatTitle";
            ChatCreateDTO chatCreateDTO = new ChatCreateDTO()
            {
                Title = title,
                UserId = Guid.CreateVersion7().ToString(),
            };
            Chat chat = chatCreateDTO.ToModel();
            chat.Id = Guid.CreateVersion7();

            _chatRepositoryMock.Setup(chatRepository => chatRepository.CreateChat(It.IsAny<Chat>(), chatCreateDTO.UserId))
                .ReturnsAsync(new ResponseResult(true, ""));

            // Act
            var result = await _chatController.Create(chatCreateDTO);

            // Assert
            CreatedAtRouteResult createdAtRouteResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            createdAtRouteResult.RouteName.Should().Be(nameof(_chatController.GetChatById));
            ChatReadDTO resultChatReadDTO = createdAtRouteResult.Value.Should().BeOfType<ChatReadDTO>().Subject;
            
            resultChatReadDTO.Title.Should().Be(title);

            _chatRepositoryMock.Verify(chatRepository => chatRepository.CreateChat(It.IsAny<Chat>(), chatCreateDTO.UserId), Times.Once);
        }


        [Fact]
        public async Task Delete_ReturnNotFound()
        {
            Guid chatId = Guid.CreateVersion7();

            _chatRepositoryMock.Setup(chatRepository => chatRepository.DeleteChat(chatId))
                .ReturnsAsync(new ResponseResult(false, ""));

            // Act
            var result = await _chatController.DeleteChat(chatId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
            _chatRepositoryMock.Verify(chatRepository => chatRepository.DeleteChat(chatId), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnNoContent()
        {
            Guid chatId = Guid.CreateVersion7();

            _chatRepositoryMock.Setup(chatRepository => chatRepository.DeleteChat(chatId))
                .ReturnsAsync(new ResponseResult(true, ""));

            // Act
            var result = await _chatController.DeleteChat(chatId);

            // Assert
            result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
            _chatRepositoryMock.Verify(chatRepository => chatRepository.DeleteChat(chatId), Times.Once);
        }
    }
}
