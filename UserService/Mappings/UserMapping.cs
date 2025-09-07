using UserService.DTOs;
using UserService.Models;

namespace UserService.Mappings
{
    public static class UserMapping
    {
        public static AppUser ToModel(this UserReadDTO userReadDTO)
        {
            return new AppUser()
            {
                Id = userReadDTO.Id.ToString(),
                UserName = userReadDTO.UserName,
                Email = userReadDTO.Email
            };
        }

        public static AppUser ToModel(this RegisterDTO registerDTO)
        {
            return new AppUser()
            {
                UserName = registerDTO.Username,
                Email = registerDTO.Email,
            };
        }

        public static AppUser ToModel(this UpdateUserDTO updateUserDTO)
        {
            return new AppUser()
            {
                UserName = updateUserDTO.Username,
                Email = updateUserDTO.Email,
            };
        }

        public static UserReadDTO ToReadDTO(this AppUser appUser, IEnumerable<string> roles)
        {
            return new UserReadDTO()
            {
                Id = new Guid(appUser.Id),
                UserName = appUser.UserName,
                Email = appUser.Email,
                Roles = roles,
            };
        }
    }
}
