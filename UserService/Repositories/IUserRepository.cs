using CodeExplainerCommon.Responses;
using UserService.Models;

namespace UserService.Repositories
{
    public interface IUserRepository
    {
        public Task<ResponseResult> Login(string username, string password);
        public Task<ResponseResult> ChangePassword(string userId, string oldPassword, string newPassword);
        public Task<ResponseResult> RegisterUser(
            AppUser user, 
            string password, 
            IEnumerable<string> roles);
        public IEnumerable<string> GetAllRoles();
        public Task<AppUser?> GetUserById(string userId);
        public Task<AppUser?> GetUserByUsername(string username);
        public Task<IEnumerable<string>> GetRoles(AppUser appUser);
        public Task<IEnumerable<AppUser>> GetAllUsers();
        public Task<ResponseResult> UpdateUser(
            string userId, 
            AppUser user, 
            IEnumerable<string> roles);
        public Task<ResponseResult> DeleteUser(string userId);

    }
}
