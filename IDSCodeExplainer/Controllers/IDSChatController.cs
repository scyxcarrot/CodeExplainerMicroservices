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

            var chatMessages = requestChatMessageDTO.ChatMessages
                .Select(chatMessageDTO =>
                {
                    if (!Enum.TryParse<ChatRole>(
                            chatMessageDTO.ChatRole, 
                            true, 
                            out var chatRole))
                    {
                        throw new ArgumentException($"chatRole = {chatMessageDTO.ChatRole} not recognized");
                    }
                    return new ChatMessage(chatRole, chatMessageDTO.TextMessage);
                })
                .ToList();

            // Add the system prompt at the beginning of the message list
            chatMessages.Insert(0, new ChatMessage(ChatRole.System, SystemPrompt));

            var chatResponse = await chatClient.GetResponseAsync(
                chatMessages,
                chatOptions);

            var responseChatMessageDTO = new ResponseChatMessageDTO()
            {
                ChatMessaage = new ChatMessageDTO()
                {
                    TextMessage = chatResponse.Text
                }
            };

            // send the response to ChatService with rabbitMQ
            // RabbitMQ has FIFO as long as we have 1 consumer, so it is ok to send like this
            
            return Ok(responseChatMessageDTO);
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
