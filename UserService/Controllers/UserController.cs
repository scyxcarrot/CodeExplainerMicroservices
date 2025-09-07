using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UserService.Constants;
using UserService.DTOs;
using UserService.Mappings;
using UserService.Repositories;
using UserService.Service;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController(
        ITokenService tokenService,
        IUserRepository userRepository) : ControllerBase
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

            var token = await tokenService.CreateToken(appUser);
            var refreshToken = await tokenService.CreateRefreshToken(appUser);
            var userReadDTO = new UserReadDTO()
            {
                Id = new Guid(appUser.Id),
                UserName = registerDTO.Username,
                Email = registerDTO.Email,
                Roles = registerDTO.Roles,
                Token = token,
                RefreshToken = refreshToken,
            };
            return CreatedAtRoute(nameof(GetUserById),
                new { userId = appUser.Id }, userReadDTO);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserReadDTO>> Login(LoginDTO loginDTO)
        {
            var result = await userRepository.Login(loginDTO.Username, loginDTO.Password);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            var appUser = await userRepository.GetUserByUsername(loginDTO.Username);
            var token = await tokenService.CreateToken(appUser);
            var refreshToken = await tokenService.CreateRefreshToken(appUser);

            var roles = await userRepository.GetRoles(appUser);
            var userReadDTO = appUser.ToReadDTO(roles);
            userReadDTO.Token = token;
            userReadDTO.RefreshToken = refreshToken;
            
            return Ok(userReadDTO);
        }

        [HttpPost("RefreshToken")]
        public async Task<ActionResult<UserReadDTO>> RefreshToken(RefreshDTO refreshDTO)
        {
            var userId = await tokenService.GetUserIdFromRefreshToken(
                refreshDTO.RefreshToken);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid refresh token");
            }

            var appUser = await userRepository.GetUserById(userId);
            if (appUser == null)
            {
                return BadRequest("User not found");
            }

            var userName = appUser.UserName;
            if (userName != refreshDTO.Username)
            {
                return BadRequest("Username does not match");
            }

            var token = await tokenService.CreateToken(appUser);
            var refreshToken = await tokenService.CreateRefreshToken(appUser);

            var roles = await userRepository.GetRoles(appUser);
            var userReadDTO = appUser.ToReadDTO(roles);
            userReadDTO.Token = token;
            userReadDTO.RefreshToken = refreshToken;

            return Ok(userReadDTO);
        }

        [HttpGet("Roles")]
        public ActionResult<IEnumerable<string>> GetAllRoles()
        {
            return Ok(userRepository.GetAllRoles());
        }

        [Authorize]
        [HttpPatch("Details/{userId}")]
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
            userReadDTO.Token = token;
            userReadDTO.RefreshToken = refreshToken;

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

            await tokenService.DeleteRefreshToken(userId);
            return NoContent();
        }
    }
}
