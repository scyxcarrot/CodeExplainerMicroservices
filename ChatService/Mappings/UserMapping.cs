using ChatService.DTOs;
using ChatService.Models;

namespace ChatService.Mappings
{
    public static class UserMapping
    {
        public static AppUser ToModel(this UserCreateDTO createUserDTO)
        {
            return new AppUser()
            {
                ExternalId = createUserDTO.ExternalId,
            };
        }

        public static UserReadDTO ToReadDTO(this AppUser appUser)
        {
            return new UserReadDTO()
            {
                Id = appUser.Id,
                ExternalId = appUser.ExternalId,
                ChatIds = appUser.Chats.Select(chat=>chat.Id),
            };
        }
    }
}
