using System.ComponentModel;
using System.Text;

using CodeExplainerCommon.DTOs;

using IDSCodeExplainer.DTOs;
using IDSCodeExplainer.HttpClients;
using IDSCodeExplainer.Services.Ingestion;

using MassTransit;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace IDSCodeExplainer.Controllers
{
    [ApiController]
    [Route("api/v1/IDSCodeExplainer/[controller]")]
    public class IDSChatController(
        ILogger<IDSChatController> logger,
        IConfiguration configuration,
        IChatClient chatClient, 
        SemanticSearch semanticSearch,
        IChatServiceClient chatServiceClient,
        IBus bus) : ControllerBase
    {
        private string MessageSystemPrompt => configuration["MessageSystemPrompt"]!;
        private string TitleSystemPrompt => configuration["TitleSystemPrompt"]!;

        [Authorize]
        [HttpPost("Message")]
        public async Task<ActionResult<string>> ChatIDSCode(
            RequestChatMessageDTO requestChatMessageDTO)
        {
            var chatOptions = new ChatOptions()
            {
                Tools = [
                    AIFunctionFactory.Create(SearchTool),
                ]
            };

            // get previous messages
            var chatReadDTO = await chatServiceClient.GetChatMessages(requestChatMessageDTO.ChatId);
            if (chatReadDTO == null) 
            {
                return BadRequest($"ChatId = {requestChatMessageDTO.ChatId} not found");
            }

            var recentMessages = chatReadDTO.Messages
                .OrderBy(m => m.MessageOrder);

            var systemMessage = new ChatMessage(ChatRole.System, MessageSystemPrompt);
            List<ChatMessage> chatMessages = new List<ChatMessage>() { systemMessage };
            foreach (MessageReadDTO recentMessage in recentMessages)
            {
                if (!ConvertStringToChatRole(recentMessage.ChatRole, out ChatRole chatRole))
                {
                    return StatusCode(500, "Invalid ChatRole string provided in the message.");
                }

                ChatMessage chatMessage = new ChatMessage(chatRole, recentMessage.TextMessage);
                chatMessages.Add(chatMessage);
            }

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

            // send the response to ChatService with rabbitMQ
            // RabbitMQ has FIFO as long as we have 1 consumer, so it is ok to send like this
            try
            {
                var chatMessageCount = chatReadDTO.Messages.Count();
                var userMessageCreateDTO = new MessageCreateDTO()
                {
                    ChatId = requestChatMessageDTO.ChatId,
                    ChatRole = "user",
                    TextMessage = requestChatMessageDTO.ChatMessage,
                    MessageOrder = chatMessageCount,
                };
                await bus.Publish(userMessageCreateDTO);

                var assistantMessageCreateDTO = new MessageCreateDTO()
                {
                    ChatId = requestChatMessageDTO.ChatId,
                    ChatRole = "assistant",
                    TextMessage = chatResponse.Text,
                    MessageOrder = chatMessageCount + 1,
                };
                await bus.Publish(assistantMessageCreateDTO);
            }
            catch (Exception ex)
            {
                logger.LogError("Could not send platform asynchronously {ExMessage}", ex.Message);
            }

            return Ok(chatResponse.Text);
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
        /// Search Qdrant for relevant code chunks
        /// </summary>
        /// <param name="searchText">The natural language query or keywords to search for relevant code documentation and source files.</param>
        /// <returns>A combined string containing the top relevant code snippets and documentation chunks.</returns>
        [Description("Searches the codebase knowledge base (Qdrant) for relevant code snippets or documentation based on a semantic query.")]
        private async Task<string> SearchTool(
            [Description("The natural language query or keywords to search for.")] string searchText)
        {
            IEnumerable<VectorSearchResult<CodeChunk>> searchResults = 
                await semanticSearch.SearchAsync(searchText, null, 10);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("--- START OF RETRIEVED CONTEXT ---");

            int index = 1;
            foreach (var searchResult in searchResults)
            {
                CodeChunk codeChunk = searchResult.Record;
                double? score = searchResult.Score;
                string typeName = codeChunk.TypeName;
                string codeDocumentId = codeChunk.CodeDocumentId;

                CodeDocument codeDocument = await semanticSearch.GetDocument(codeDocumentId);

                // Add a section for each chunk with metadata
                stringBuilder.AppendLine($"\n### RESULT {index} (Score: {score:F4})");
                stringBuilder.AppendLine($"Filename: {codeDocument.RelativePath}");
                stringBuilder.AppendLine($"Class: {typeName}");
                stringBuilder.AppendLine("CODE SNIPPET:");
                stringBuilder.AppendLine("```csharp");
                stringBuilder.AppendLine(codeChunk.CodeSnippet);
                stringBuilder.AppendLine("```");

                index++;
            }

            // 4. Handle Case: No Results Found
            if (!searchResults.Any())
            {
                stringBuilder.AppendLine("No relevant code chunks or documentation were found in the knowledge base.");
            }

            // Add a clear ending delimiter
            stringBuilder.AppendLine("\n--- END OF RETRIEVED CODEBASE CONTEXT ---");

            return stringBuilder.ToString();
        }
    }
}
