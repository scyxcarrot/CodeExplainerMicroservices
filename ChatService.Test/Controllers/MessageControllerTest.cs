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
    public class MessageControllerTest
    {
        private readonly Mock<IMessageRepository> _messageRepositoryMock;

        // SUT
        private readonly MessageController _messageController;

        public MessageControllerTest()
        {
            _messageRepositoryMock = new Mock<IMessageRepository>();
            _messageController = new MessageController(_messageRepositoryMock.Object);
        }

        [Fact]
        public async Task GetChatById_ReturnsNotFoundIfNull()
        {
            // arrange
            Guid messageId = Guid.CreateVersion7();
            _messageRepositoryMock
                .Setup(messageRepository => messageRepository.GetMessageById(It.IsAny<Guid>()))
                .ReturnsAsync((Message) null);

            // act
            var result = await _messageController.GetMessageById(messageId);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
            _messageRepositoryMock
                .Verify(messageRepository => messageRepository.GetMessageById(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GetChatById_ReturnsOkObjectResult()
        {
            // arrange
            Guid messageId = Guid.CreateVersion7();
            Message message = new Message()
            {
                ChatRole = "user",
                TextMessage = "SampleMessage"
            };

            _messageRepositoryMock
                .Setup(messageRepository => messageRepository.GetMessageById(It.IsAny<Guid>()))
                .ReturnsAsync(message);

            // act
            var result = await _messageController.GetMessageById(messageId);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
            OkObjectResult okObjectResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            MessageReadDTO resultMessageReadDTO = okObjectResult.Value.Should().BeOfType<MessageReadDTO>().Subject;
            resultMessageReadDTO.ChatRole.Should().Be(message.ChatRole);
            resultMessageReadDTO.TextMessage.Should().Be(message.TextMessage);
            _messageRepositoryMock
                .Verify(messageRepository => messageRepository.GetMessageById(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task Create_ReturnsStatusCode500IfFailed()
        {
            // arrange
            MessageCreateDTO messageCreateDTO = new MessageCreateDTO()
            {
                ChatId = Guid.CreateVersion7(),
                ChatRole = "user",
                MessageOrder = 0,
                TextMessage = "SampleMessage",
            };
            _messageRepositoryMock
                .Setup(messageRepository => messageRepository.CreateMessage(It.IsAny<Message>()))
                .ReturnsAsync(new ResponseResult(false, ""));

            // act
            var result = await _messageController.Create(messageCreateDTO);

            // Assert
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
            _messageRepositoryMock
                .Verify(messageRepository => messageRepository.CreateMessage(It.IsAny<Message>()), Times.Once);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtRoute()
        {
            // arrange
            MessageCreateDTO messageCreateDTO = new MessageCreateDTO()
            {
                ChatId = Guid.CreateVersion7(),
                ChatRole = "user",
                MessageOrder = 0,
                TextMessage = "SampleMessage",
            };
            _messageRepositoryMock
                .Setup(messageRepository => messageRepository.CreateMessage(It.IsAny<Message>()))
                .ReturnsAsync(new ResponseResult(true, ""));

            // act
            var result = await _messageController.Create(messageCreateDTO);

            // Assert
            CreatedAtRouteResult createdAtRouteResult = result.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
            createdAtRouteResult.StatusCode.Should().Be(201);
            createdAtRouteResult.RouteName.Should().Be("GetMessageById");
            MessageReadDTO resultMessageReadDTO = createdAtRouteResult.Value.Should().BeOfType<MessageReadDTO>().Subject;
            resultMessageReadDTO.ChatId.Should().Be(messageCreateDTO.ChatId);
            resultMessageReadDTO.ChatRole.Should().Be(messageCreateDTO.ChatRole);
            resultMessageReadDTO.MessageOrder.Should().Be(messageCreateDTO.MessageOrder);
            resultMessageReadDTO.TextMessage.Should().Be(messageCreateDTO.TextMessage);
            _messageRepositoryMock
                .Verify(messageRepository => messageRepository.CreateMessage(It.IsAny<Message>()), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnBadRequestIfUpdateFailed()
        {
            Guid messageId = Guid.CreateVersion7();
            string title = "ChatTitle";
            MessageUpdateDTO messageUpdateDTO = new MessageUpdateDTO()
            {
                ChatId = Guid.CreateVersion7(),
                ChatRole = "user",
                MessageOrder = 0,
                TextMessage = "SampleMessage",
            };
            _messageRepositoryMock.Setup(messageRepository => messageRepository.UpdateMessage(It.IsAny<Message>()))
                .ReturnsAsync(new ResponseResult(false, ""));

            // Act
            var result = await _messageController.Update(messageId, messageUpdateDTO);

            // Assert
            BadRequestObjectResult badRequestObjectResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestObjectResult.StatusCode.Should().Be(400);
            _messageRepositoryMock.Verify(messageRepository => messageRepository.UpdateMessage(It.IsAny<Message>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_ReturnOkRequestIfUpdateSuccess()
        {
            Guid messageId = Guid.CreateVersion7();
            string title = "ChatTitle";
            MessageUpdateDTO messageUpdateDTO = new MessageUpdateDTO()
            {
                ChatId = Guid.CreateVersion7(),
                ChatRole = "user",
                MessageOrder = 0,
                TextMessage = "SampleMessage",
            };
            Message message = messageUpdateDTO.ToModel(messageId);

            _messageRepositoryMock.Setup(messageRepository => messageRepository.UpdateMessage(It.IsAny<Message>()))
                .ReturnsAsync(new ResponseResult(true, ""));
            _messageRepositoryMock.Setup(messageRepository => messageRepository.GetMessageById(It.IsAny<Guid>()))
                .ReturnsAsync(message);
            // Act
            var result = await _messageController.Update(messageId, messageUpdateDTO);

            // Assert
            OkObjectResult okObjectResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            MessageReadDTO resultChatReadDTO = okObjectResult.Value.Should().BeOfType<MessageReadDTO>().Subject;
            resultChatReadDTO.ChatId.Should().Be(messageUpdateDTO.ChatId);
            resultChatReadDTO.ChatRole.Should().Be(messageUpdateDTO.ChatRole);
            resultChatReadDTO.MessageOrder.Should().Be(messageUpdateDTO.MessageOrder);
            resultChatReadDTO.TextMessage.Should().Be(messageUpdateDTO.TextMessage);
            _messageRepositoryMock.Verify(messageRepository => messageRepository.UpdateMessage(It.IsAny<Message>()), 
                Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnNotFound()
        {
            Guid chatId = Guid.CreateVersion7();

            _messageRepositoryMock.Setup(chatRepository => chatRepository.DeleteMessage(chatId))
                .ReturnsAsync(new ResponseResult(false, ""));

            // Act
            var result = await _messageController.DeleteMessage(chatId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
            _messageRepositoryMock.Verify(chatRepository => chatRepository.DeleteMessage(chatId), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnNoContent()
        {
            Guid chatId = Guid.CreateVersion7();

            _messageRepositoryMock.Setup(chatRepository => chatRepository.DeleteMessage(chatId))
                .ReturnsAsync(new ResponseResult(true, ""));

            // Act
            var result = await _messageController.DeleteMessage(chatId);

            // Assert
            result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
            _messageRepositoryMock.Verify(chatRepository => chatRepository.DeleteMessage(chatId), Times.Once);
        }
    }
}
