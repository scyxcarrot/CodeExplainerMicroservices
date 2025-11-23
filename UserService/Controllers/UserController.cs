using CodeExplainerCommon.Constants;
using CodeExplainerCommon.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UserService.Constants;
using UserService.DTOs;
using UserService.HttpClients;
using UserService.Mappings;
using UserService.Repositories;
using UserService.Service;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/v1/UserService/[controller]")]
    public class UserController(
        ITokenService tokenService,
        IUserRepository userRepository,
        IChatServiceClient chatServiceClient) : ControllerBase
    {
        [Authorize]
        [HttpGet("{userId}", Name = "GetUserById")]
        public async Task<ActionResult<UserReadDTO>> GetUserById(string userId)
        {
            var appUser = await userRepository.GetUserById(userId);
            if (appUser == null)
            {
                return NotFound();
            }

            var roles = await userRepository.GetRoles(appUser);
            return Ok(appUser.ToReadDTO(roles));
        }

        [HttpPost("Register")]
        public async Task<ActionResult<UserReadDTO>> Register(RegisterDTO registerDTO)
        {
            var appUser = registerDTO.ToModel();
            var result = await userRepository.RegisterUser(
                appUser, 
                registerDTO.Password, 
                registerDTO.Roles);
            if (!result.Success)
            {
                return StatusCode(500, result.Message);
            }

            var userReadDTO = new UserReadDTO()
            {
                Id = new Guid(appUser.Id),
                UserName = registerDTO.Username,
                Email = registerDTO.Email,
                Roles = registerDTO.Roles,
            };

            // Send the created user to ChatService by HTTP
            // this ensures the user is created on that side instantly, it cannot wait
            var userCreated = new UserCreatedDTO { Id = appUser.Id };
            await chatServiceClient.NotifyUserCreated(userCreated);
            return CreatedAtRoute(nameof(GetUserById),
                new { userId = appUser.Id }, userReadDTO);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserReadDTO>> Login(LoginDTO loginDTO)
        {
            var result = await userRepository.Login(loginDTO.Email, loginDTO.Password);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            var appUser = await userRepository.GetUserByEmail(loginDTO.Email);
            var token = await tokenService.CreateToken(appUser);
            var refreshToken = await tokenService.CreateRefreshToken(appUser);

            var roles = await userRepository.GetRoles(appUser);
            var userReadDTO = appUser.ToReadDTO(roles);

            // Set the tokens as cookies
            SetTokenCookies(token, refreshToken);
            
            return Ok(userReadDTO);
        }

        [HttpPost("RefreshToken")]
        public async Task<ActionResult<UserReadDTO>> RefreshToken()
        {
            var refreshToken = Request.Cookies[Token.RefreshToken];

            var userId = await tokenService.GetUserIdFromRefreshToken(
                refreshToken);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid refresh token");
            }

            var appUser = await userRepository.GetUserById(userId);
            if (appUser == null)
            {
                return BadRequest("User not found");
            }

            var token = await tokenService.CreateToken(appUser);
            var newRefreshToken = await tokenService.CreateRefreshToken(appUser);

            var roles = await userRepository.GetRoles(appUser);
            var userReadDTO = appUser.ToReadDTO(roles);

            // Set the tokens as cookies
            SetTokenCookies(token, newRefreshToken);

            return Ok(userReadDTO);
        }

        [HttpGet("Roles")]
        public ActionResult<IEnumerable<string>> GetAllRoles()
        {
            return Ok(userRepository.GetAllRoles());
        }

        [Authorize]
        [HttpPatch("{userId}")]
        public async Task<ActionResult<UserReadDTO>> UpdateUser(
            string userId, 
            UpdateUserDTO updateUserDTO)
        {
            var appUser = updateUserDTO.ToModel();
            appUser.Id = userId;
            
            var result = await userRepository.UpdateUser(userId, appUser, updateUserDTO.Roles);
            if (!result.Success)
            {
                return StatusCode(500, result.Message);
            }

            var token = await tokenService.CreateToken(appUser);
            var refreshToken = await tokenService.CreateRefreshToken(appUser);

            var roles = await userRepository.GetRoles(appUser);
            var userReadDTO = appUser.ToReadDTO(roles);

            // Set the tokens as cookies
            SetTokenCookies(token, refreshToken);

            return Ok(userReadDTO);
        }

        [Authorize]
        [HttpPost("ChangePassword/{userId}")]
        public async Task<ActionResult> ChangeUserPassword(
            string userId,
            ChangePasswordDTO changePasswordDTO)
        {
            var result = await userRepository.ChangePassword(
                userId, 
                changePasswordDTO.OldPassword, 
                changePasswordDTO.NewPassword);

            if (!result.Success)
            {
                return StatusCode(500, result.Message);
            }

            return NoContent();
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserReadDTO>>> GetAllUsers()
        {
            var appUsers = await userRepository.GetAllUsers();
            appUsers = appUsers.ToList();
            var userReadDTOs = new List<UserReadDTO>();
            foreach (var appUser in appUsers)
            {
                var roles = await userRepository.GetRoles(appUser);
                var userReadDTO = appUser.ToReadDTO(roles);
                userReadDTOs.Add(userReadDTO);
            }
            
            return Ok(userReadDTOs);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var result = await userRepository.DeleteUser(userId);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            // Send the deleted user to ChatService by HTTP
            // this ensures the user is deleted on that side instantly, it cannot wait
            var deleteUserInChatServiceSuccess = await chatServiceClient.NotifyUserDeleted(userId);
            if (deleteUserInChatServiceSuccess)
            {
                return NoContent();
            }

            var failureMessage = "User deleted, but related data could not be removed: ";
            if (!deleteUserInChatServiceSuccess)
            {
                failureMessage += "Failed to notify ChatService of user deletion;";
            }
            return BadRequest(failureMessage);
        }

        [Authorize]
        [HttpPost("Logout")]
        public ActionResult Logout()
        {
            Response.Cookies.Delete(Token.AccessToken);
            Response.Cookies.Delete(Token.RefreshToken);

            return NoContent();
        }

        private void SetTokenCookies(string token, string refreshToken)
        {
            var tokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Domain = ".code-explainer.com",
                Expires = DateTime.UtcNow.AddSeconds(Token.AccessTokenExpiryTime)
            };

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Domain = ".code-explainer.com",
                Expires = DateTime.UtcNow.AddSeconds(Token.RefreshTokenExpiryTime)
            };

            // Set the cookies in the response
            Response.Cookies.Append(Token.AccessToken, token, tokenOptions);
            Response.Cookies.Append(Token.RefreshToken, refreshToken, refreshTokenOptions);
        }
    }
}
