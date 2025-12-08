using System;
using System.ComponentModel;

using CodeExplainerCommon.DTOs;

using IDSCodeExplainer.DTOs;
using IDSCodeExplainer.HttpClients;
using IDSCodeExplainer.Services;

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
        FileService fileService,
        IChatServiceClient chatServiceClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
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
                //Tools = [
                //    AIFunctionFactory.Create(ReadFileTool), 
                //    AIFunctionFactory.Create(FindFileTool)
                //]
            };

            // get previous messages
            var chatReadDTO = await chatServiceClient.GetChatMessages(requestChatMessageDTO.ChatId);
            if (chatReadDTO == null) 
            {
                return BadRequest($"ChatId = {requestChatMessageDTO.ChatId} not found");
            }

            // take only the last 4 messages
            var recentMessageList = chatReadDTO.Messages
                .OrderBy(m => m.MessageOrder)
                .TakeLast(4)
                .Select(messageReadDTO => $"{messageReadDTO.ChatRole}: {messageReadDTO.TextMessage}");
            var previousMessages = string.Join(Environment.NewLine, recentMessageList).Trim();

            // get context
            var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(requestChatMessageDTO.Message);
            var results = movies.SearchEmbeddingAsync(queryEmbedding, 10, new VectorSearchOptions<Movie>()
            {
                VectorProperty = movie => movie.DescriptionEmbedding
            });

            var searchedResult = new HashSet<string>();
            var references = new HashSet<string>();
            await foreach (var result in results)
            {
                searchedResult.Add($"[{result.Record.Title}]: {result.Record.Description} '{result.Record.Reference}'");

                var score = result.Score ?? 0;
                var percent = (score * 100).ToString("F2");
                references.Add($"[{percent}%] {result.Record.Reference}");
            }
            var context = string.Join(Environment.NewLine, searchedResult);

            var prompt = $"""
                          Current context:
                          {context}

                          Previous conversations:
                          this is the area of your memory for referred questions.
                          {previousMessages}

                          Rules:
                          Make sure you never expose our sensitive information such as tokens to the user as part of the answer.
                          1. Based on the current context and our previous conversation, please answer the following question.
                          2. If in the question user asked based on previous conversation, a referred question, use your memory first.
                          3. If you don't know, say you don't know based on the provided information.

                          User question: {requestChatMessageDTO.Message}

                          Answer:
                          """;

            var systemMessage = new ChatMessage(ChatRole.System, MessageSystemPrompt);
            var userMessage = new ChatMessage(ChatRole.User, prompt);
            var chatMessages = new List<ChatMessage> { systemMessage, userMessage };

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
                    ChatRole = "üser",
                    TextMessage = requestChatMessageDTO.Message,
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

            string finalResponse = chatResponse.Text;
            if (references.Count > 0)
            {
                finalResponse += "\n\nReferences used:";
                foreach (var reference in references)
                {
                    finalResponse += $"\n- {reference}";
                }
            }

            return Ok(finalResponse);
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
