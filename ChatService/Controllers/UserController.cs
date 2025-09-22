using ChatService.DTOs;
using ChatService.Mappings;
using ChatService.Repositories;
using CodeExplainerCommon.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController(
        IUserRepository userRepository) : ControllerBase
    {
        [Authorize]
        [HttpGet("{externalUserId}", Name = "GetUserByExternalId")]
        public async Task<ActionResult<UserReadDTO>> GetUserByExternalId(string externalUserId)
        {
            var appUser = await userRepository.GetUserByExternalId(externalUserId);
            if (appUser == null)
            {
                return NotFound();
            }

            return Ok(appUser.ToReadDTO());
        }

        [HttpPost]
        public async Task<ActionResult<UserReadDTO>> Create(UserCreatedDTO userCreatedDTO)
        {
            var appUser = userCreatedDTO.ToModel();
            var result = await userRepository.CreateUser(appUser);
            if (!result.Success)
            {
                return StatusCode(500, result.Message);
            }

            var userReadDTO = new UserReadDTO()
            {
                Id = appUser.Id,
                ExternalId = appUser.ExternalId,
                ChatIds = appUser.Chats?.Select(chat=>chat.Id),
            };

            return CreatedAtRoute(nameof(GetUserByExternalId),
                new { userId = appUser.Id }, userReadDTO);
        }

        [Authorize]
        [HttpDelete("{externalUserId}")]
        public async Task<IActionResult> DeleteUserByExternalId(string externalUserId)
        {
            var result = await userRepository.DeleteUserByExternalId(externalUserId);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return NoContent();
        }
    }
}
