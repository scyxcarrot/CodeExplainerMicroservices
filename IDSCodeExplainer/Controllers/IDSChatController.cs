using System.ComponentModel;

using CodeExplainerCommon.DTOs;

using IDSCodeExplainer.DTOs;
using IDSCodeExplainer.HttpClients;
using IDSCodeExplainer.Services;

using MassTransit;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace IDSCodeExplainer.Controllers
{
    [ApiController]
    [Route("api/v1/IDSCodeExplainer/[controller]")]
    public class IDSChatController(
        ILogger<IDSChatController> logger,
        IConfiguration configuration,
        IChatClient chatClient, 
        FileService fileService,
        IChatServiceClient chatServiceClient,
        IBus bus) : ControllerBase
    {
        private string MessageSystemPrompt => configuration["MessageSystemPrompt"]!;
        private string TitleSystemPrompt => configuration["TitleSystemPrompt"]!;

        [Authorize]
        [HttpPost("Message")]
        public async Task<ActionResult<ChatMessageDTO>> ChatIDSCode(
            RequestChatMessageDTO requestChatMessageDTO)
        {
            var chatOptions = new ChatOptions()
            {
                Tools = [
                    AIFunctionFactory.Create(ReadFileTool), 
                    AIFunctionFactory.Create(FindFileTool)
                ]
            };

            var chatMessages = new List<ChatMessage>();
            foreach (var chatMessageDTO in requestChatMessageDTO.ChatMessages)
            {
                if (!ConvertStringToChatRole(chatMessageDTO.ChatRole, out var chatRole))
                {
                    // Instead of throwing an exception, return a BadRequest
                    return BadRequest($"Invalid ChatRole: {chatMessageDTO.ChatRole}");
                }
                chatMessages.Add(new ChatMessage(chatRole, chatMessageDTO.TextMessage));
            }

            // Add the system prompt at the beginning of the message list
            chatMessages.Insert(0, new ChatMessage(ChatRole.System, MessageSystemPrompt));

            ChatResponse? chatResponse;
            try
            {
                chatResponse = await chatClient.GetResponseAsync(
                    chatMessages,
                    chatOptions);
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message);
            }

            var responseChatMessageDTO = new ChatMessageDTO()
            {
                ChatRole = "Assistant",
                TextMessage = chatResponse.Text,
            };

            // send the response to ChatService with rabbitMQ
            // RabbitMQ has FIFO as long as we have 1 consumer, so it is ok to send like this
            try
            {
                var chatMessageCount = requestChatMessageDTO.ChatMessages.Count;
                var userMessageCreateDTO = new MessageCreateDTO()
                {
                    ChatId = requestChatMessageDTO.ChatId,
                    ChatRole = requestChatMessageDTO.ChatMessages[chatMessageCount - 1].ChatRole,
                    TextMessage = requestChatMessageDTO.ChatMessages[chatMessageCount - 1].TextMessage,
                    MessageOrder = chatMessageCount - 1,
                };
                await bus.Publish(userMessageCreateDTO);

                var assistantMessageCreateDTO = new MessageCreateDTO()
                {
                    ChatId = requestChatMessageDTO.ChatId,
                    ChatRole = responseChatMessageDTO.ChatRole,
                    TextMessage = responseChatMessageDTO.TextMessage,
                    MessageOrder = chatMessageCount,
                };
                await bus.Publish(assistantMessageCreateDTO);
            }
            catch (Exception ex)
            {
                logger.LogError("Could not send platform asynchronously {ExMessage}", ex.Message);
            }

            return Ok(responseChatMessageDTO);
        }

        // User is expected to prompt and get the chat title, then create the chat manually
        // after that, get the chatId and prompt
        [Authorize]
        [HttpPost("Title")]
        public async Task<ActionResult<ChatReadDTO>> GetChatTitle(
            RequestChatTitleDTO requestChatTitleDTO)
        {
            var chatOptions = new ChatOptions();

            var chatMessages = new List<ChatMessage>();
            chatMessages.Add(new ChatMessage(ChatRole.System, TitleSystemPrompt));
            chatMessages.Add(new ChatMessage(ChatRole.User, requestChatTitleDTO.ChatMessage));

            var chatResponse = await chatClient.GetResponseAsync(
                chatMessages,
                chatOptions);

            var chatCreateDTO = new ChatCreateDTO() 
            { 
                Title = chatResponse.Text,
                UserId = requestChatTitleDTO.UserId,
            };
            var chatReadDTO = await chatServiceClient.CreateChat(chatCreateDTO);
            return Ok(chatReadDTO);
        }

        private bool ConvertStringToChatRole(string chatRoleString, out ChatRole chatRole)
        {
            chatRole = chatRoleString.ToLowerInvariant() switch
            {
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                "system" => ChatRole.System,
                "tool" => ChatRole.Tool,
                _ => default // Default case for unknown strings
            };

            return chatRole != default;
        }

        /// <summary>
        /// Reads the content of a specified file.
        /// </summary>
        /// <param name="filepath">The name of the file to read.</param>
        /// <returns>The entire content of the file.</returns>
        [Description("Reads the entire content of a file from the file system.")]
        private async Task<string> ReadFileTool(
            [Description("The name of the file to read. Provide the full filepath and extension.")] string filepath)
        {
            return await fileService.ReadFileContentAsync(filepath); // Call your service method
        }

        /// <summary>
        /// Return the list of full file path based on the search term
        /// </summary>
        /// <param name="searchTerm">The name of the file to read.</param>
        /// <returns>IEnumerable of full file path that matches the search term.</returns>
        [Description("Return the list of full file path based on the search term.")]
        private async Task<IEnumerable<string>> FindFileTool(
            [Description("Search term to look for")] string searchTerm)
        {
            return fileService.FindFile(searchTerm); // Call your service method
        }
    }
}
