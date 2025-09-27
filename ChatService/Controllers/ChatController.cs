using ChatService.Mappings;
using ChatService.Models;
using ChatService.Repositories;
using CodeExplainerCommon.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ChatController(
        IChatRepository chatRepository) : ControllerBase
    {
        [HttpGet("{chatId}", Name = "GetChatById")]
        public async Task<ActionResult<ChatReadDTO>> GetChatById(Guid chatId)
        {
            var chat = await chatRepository.GetChat(chatId);
            if (chat == null)
            {
                return NotFound();
            }

            return Ok(chat.ToReadDTO());
        }

        [HttpGet("User/{userExternalId}")]
        public async Task<ActionResult<IEnumerable<ChatReadDTO>>> GetAllChats(string userExternalId)
        {
            var chats = await chatRepository
                .GetAllChatsByUserExternalId(userExternalId);
            return Ok(chats.Select(c => c.ToReadDTO()));
        }

        [HttpPatch("{chatId}")]
        public async Task<ActionResult<ChatReadDTO>> Update(
            Guid chatId,
            ChatUpdateDTO chatUpdateDTO)
        {
            var chat = chatUpdateDTO.ToModel(chatId);
            var result = await chatRepository.UpdateChat(chat);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            var newChat = await chatRepository.GetChat(chatId);

            return Ok(newChat.ToReadDTO());
        }

        [HttpPost]
        public async Task<ActionResult<ChatReadDTO>> Create(ChatCreateDTO chatCreateDTO)
        {
            var chat = chatCreateDTO.ToModel();
            var result = await chatRepository.CreateChat(chat, chatCreateDTO.UserId);
            if (!result.Success)
            {
                return StatusCode(500, result.Message);
            }

            var chatReadDTO = new ChatReadDTO()
            {
                Id = chat.Id,
                Messages = new List<MessageReadDTO>(),
                LastUpdated = chat.LastUpdated,
                Title = chat.Title,
            };

            return CreatedAtRoute(nameof(GetChatById),
                new { chatId = chat.Id }, chatReadDTO);
        }

        [HttpDelete("{chatId}")]
        public async Task<IActionResult> DeleteChat(Guid chatId)
        {
            var result = await chatRepository.DeleteChat(chatId);
            if (!result.Success)
            {
                return NotFound(result.Message);
            }

            return NoContent();
        }
    }
}
