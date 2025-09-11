using System.ComponentModel;

using IDSCodeExplainer.DTOs;
using IDSCodeExplainer.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace IDSCodeExplainer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class IDSChatController(
        IConfiguration configuration,
        IChatClient chatClient, 
        FileService fileService) : ControllerBase
    {
        private string SystemPrompt => configuration["SystemPrompt"]!;

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ResponseChatMessageDTO>> ChatIDSCode(
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
            chatMessages.Insert(0, new ChatMessage(ChatRole.System, SystemPrompt));

            var chatResponse = await chatClient.GetResponseAsync(
                chatMessages,
                chatOptions);

            var responseChatMessageDTO = new ResponseChatMessageDTO()
            {
                ChatMessaage = new ChatMessageDTO()
                {
                    ChatRole = "Assistant",
                    TextMessage = chatResponse.Text,
                }
            };

            // send the response to ChatService with rabbitMQ
            // RabbitMQ has FIFO as long as we have 1 consumer, so it is ok to send like this
            
            return Ok(responseChatMessageDTO);
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
