using ChatService.Mappings;
using ChatService.Models;
using ChatService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MessageController(
        IMessageRepository messageRepository) : ControllerBase
    {
        [HttpGet("{messageId}", Name = "GetMessageById")]
        public async Task<ActionResult<MessageReadDTO>> GetMessageById(Guid messageId)
        {
            var message = await messageRepository.GetMessageById(messageId);
            if (message == null)
            {
                return NotFound();
            }
            return Ok(message.ToReadDTO());
        }

        [HttpPost]
        public async Task<ActionResult<ChatReadDTO>> Create(MessageCreateDTO messageCreateDTO)
        {
            var message = messageCreateDTO.ToModel();
            var result = await messageRepository.CreateMessage(message);
            if (!result.Success)
            {
                return StatusCode(500, result.Message);
            }

            var messageReadDTO = message.ToReadDTO();

            return CreatedAtRoute(nameof(GetMessageById),
                new { messageId = message.Id }, messageReadDTO);
        }

        [HttpPatch("{messageId}")]
        public async Task<ActionResult<ChatReadDTO>> Update(
            Guid messageId,
            MessageUpdateDTO messageUpdateDTO)
        {
            var message = messageUpdateDTO.ToModel(messageId);
            var result = await messageRepository.UpdateMessage(message);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            var newMessage = await messageRepository.GetMessageById(messageId);

            return Ok(newMessage.ToReadDTO());
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            var result = await messageRepository.DeleteMessage(messageId);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return NoContent();
        }
    }
}
