using CodeExplainerCommon.DTOs;

using FluentAssertions;

using IDSCodeExplainer.Controllers;
using IDSCodeExplainer.DTOs;
using IDSCodeExplainer.HttpClients;
using IDSCodeExplainer.Services.Ingestion;

using MassTransit;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;

using Moq;

namespace IDSCodeExplainer.Tests.Controllers
{
    public class IDSChatControllerTests
    {
        private readonly IDSChatController _idsChatController;
        private readonly Mock<ILogger<IDSChatController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IChatClient> _mockChatClient;
        private readonly Mock<SemanticSearch> _mockSemanticSearch;
        private readonly Mock<IChatServiceClient> _mockChatServiceClient;
        private readonly Mock<IBus> _mockBus;

        public IDSChatControllerTests()
        {
            _mockLogger = new Mock<ILogger<IDSChatController>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockChatClient = new Mock<IChatClient>();
            _mockChatServiceClient = new Mock<IChatServiceClient>();
            _mockBus = new Mock<IBus>();

            var mockDocumentCollection = new Mock<VectorStoreCollection<Guid, CodeDocument>>();
            var mockChunkCollection = new Mock<VectorStoreCollection<Guid, CodeChunk>>();

            _mockSemanticSearch = new Mock<SemanticSearch>(mockDocumentCollection.Object, mockChunkCollection.Object);

            _mockConfig.Setup(c => c["MessageSystemPrompt"]).Returns("You are a helpful assistant.");
            _mockConfig.Setup(c => c["TitleSystemPrompt"]).Returns("Create a title.");

            _idsChatController = new IDSChatController(
                _mockLogger.Object,
                _mockConfig.Object,
                _mockChatClient.Object,
                _mockSemanticSearch.Object,
                _mockChatServiceClient.Object,
                _mockBus.Object);
        }

        [Fact]
        public async Task ChatIDSCode_ReturnString()
        {
            // Arrange
            RequestChatMessageDTO requestChatMessageDTO = new RequestChatMessageDTO()
            {
                ChatMessage = "Hi, how are you?",
                ChatId = Guid.CreateVersion7(),
            };

            string responseString = "I am fine thank you";
            _mockChatServiceClient
                .Setup(chatServiceClient => chatServiceClient.GetChatMessages(It.IsAny<Guid>()))
                .ReturnsAsync(new ChatReadDTO() {Messages = new List<MessageReadDTO>()});
            _mockChatClient
                .Setup(chatClient =>
                    chatClient.GetResponseAsync(
                        It.IsAny<IEnumerable<ChatMessage>>(), 
                        It.IsAny<ChatOptions>(), 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseString)));
            _mockBus
                .Setup(bus => bus.Publish(
                    It.IsAny<MessageCreateDTO>(), 
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var response = await _idsChatController.ChatIDSCode(requestChatMessageDTO);

            // Assert
            var okObjectResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
            string? stringResponse = okObjectResult.Value.Should().BeOfType<string>().Subject;
            stringResponse.Should().Be(responseString);
        }

        [Fact]
        public async Task GetChatTitle_ReturnChatReadDTO()
        {
            // Arrange
            RequestChatTitleDTO requestChatTitleDTO = new RequestChatTitleDTO()
            {
                ChatMessage = "Hi, How are you?",
                UserId = Guid.CreateVersion7().ToString(),
            };

            string sampleTitle = "Sample Title";
            _mockChatClient
                .Setup(chatClient =>
                    chatClient.GetResponseAsync(
                        It.IsAny<IEnumerable<ChatMessage>>(),
                        It.IsAny<ChatOptions>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, sampleTitle)));
            _mockChatServiceClient
                .Setup(chatServiceClient => chatServiceClient.CreateChat(It.IsAny<ChatCreateDTO>()))
                .ReturnsAsync(new ChatReadDTO() { Messages = new List<MessageReadDTO>(), Title = sampleTitle});

            // Act
            var response = await _idsChatController.GetChatTitle(requestChatTitleDTO);

            // Assert
            var okObjectResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
            ChatReadDTO resultChatReadDTO = okObjectResult.Value.Should().BeOfType<ChatReadDTO>().Subject;
            resultChatReadDTO.Title.Should().Be(sampleTitle);
        }
    }
}
