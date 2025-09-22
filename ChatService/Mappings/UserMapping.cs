using ChatService.DTOs;
using ChatService.Models;
using CodeExplainerCommon.DTOs;

namespace ChatService.Mappings
{
    public static class UserMapping
    {
        public static AppUser ToModel(this UserCreatedDTO userCreatedDTO)
        {
            return new AppUser()
            {
                ExternalId = userCreatedDTO.Id,
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
