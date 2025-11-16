using System.Data;
using System.Reflection;
using CodeExplainerCommon.Responses;
using Microsoft.AspNetCore.Identity;

using UserService.Constants;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Repositories
{
    public class UserRepository(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager) : IUserRepository
    {
        public async Task<ResponseResult> Login(string email, string password)
        {
            var userFound = await userManager.FindByEmailAsync(email);
            if (userFound == null)
            {
                return new ResponseResult(false, "Login failed");
            }

            var signInResult = await signInManager.CheckPasswordSignInAsync(
                userFound,
                password,
                false);
            if (!signInResult.Succeeded)
            {
                return new ResponseResult(false, "Login failed");
            }

            return new ResponseResult(true, "Login Success");
        }

        public async Task<ResponseResult> ChangePassword(string userId, string oldPassword, string newPassword)
        {
            var appUser = await userManager.FindByIdAsync(userId);
            if (appUser == null)
            {
                return new ResponseResult(false, $"User {userId} not found.");
            }

            var result = await userManager.ChangePasswordAsync(
                appUser,
                oldPassword,
                newPassword
            );

            if (result.Succeeded)
            {
                // Invalidate user's security stamp to invalidate existing tokens (optional but recommended for security)
                await userManager.UpdateSecurityStampAsync(appUser);

                return new ResponseResult(true, "Password Changed");
            }

            return new ResponseResult(false, 
                string.Join(", ", result.Errors.Select(x => x.Description)));
        }

        public async Task<ResponseResult> RegisterUser(
            AppUser appUser, 
            string password,
            IEnumerable<string> roles)
        {
            var appUserFound = await userManager.FindByEmailAsync(appUser.Email);
            if (appUserFound != null)
            {
                return new ResponseResult(
                    false, $"Email {appUser.Email} already registered");
            }

            var identityResult = await userManager.CreateAsync(appUser, password);
            if (!identityResult.Succeeded)
            {
                return new ResponseResult(identityResult.Succeeded,
                    identityResult.Errors.Select(x => x.Description));
            }

            foreach (var role in roles)
            {
                var roleValid = GetAllRoles().Any(r => r == role);
                if (!roleValid)
                {
                    return new ResponseResult(false, $"Role {role} is not valid");
                }
            }

            var roleResult = await userManager.AddToRolesAsync(appUser, roles);
            return new ResponseResult(roleResult.Succeeded,
                roleResult.Errors.Select(x => x.Description));
        }

        public IEnumerable<string> GetAllRoles()
        {
            var roleFields = typeof(Role).GetFields(
                    BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            var allRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            allRoles = roleFields
                .Select(roleField => (string)roleField.GetValue(null))
                .ToHashSet();
            return allRoles;
        }

        public async Task<AppUser?> GetUserById(string userId)
        {
            var appUser = await userManager.FindByIdAsync(userId);
            return appUser;
        }

        public async Task<AppUser?> GetUserByEmail(string email)
        {
            var appUser = await userManager.FindByEmailAsync(email);
            return appUser;
        }

        public async Task<IEnumerable<string>> GetRoles(AppUser appUser)
        {
            var roles = await userManager.GetRolesAsync(appUser);
            return roles;
        }

        public async Task<IEnumerable<AppUser>> GetAllUsers()
        {
            var allUsers = userManager.Users;
            return allUsers;
        }

        public async Task<ResponseResult> UpdateUser(
            string userId, 
            AppUser user, 
            IEnumerable<string> roles)
        {
            var appUser = await userManager.FindByIdAsync(userId);
            if (appUser == null)
            {
                return new ResponseResult(false, $"User {userId} not found.");
            }

            appUser.Email = user.Email;
            appUser.UserName = user.UserName;

            var result = await userManager.UpdateAsync(appUser);
            if (!result.Succeeded)
            {
                return new ResponseResult(false, 
                    result.Errors.Select(x=>x.Description).ToList());
            }

            foreach (var role in roles)
            {
                var roleValid = GetAllRoles().Any(r => r == role);
                if (!roleValid)
                {
                    return new ResponseResult(false, $"Role {role} is not valid");
                }
            }

            var currentUserRoles = await userManager.GetRolesAsync(appUser);
            await userManager.RemoveFromRolesAsync(appUser, currentUserRoles);
            await userManager.AddToRolesAsync(appUser, roles);

            return new ResponseResult(true, $"User {userId} updated");
        }

        public async Task<ResponseResult> DeleteUser(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ResponseResult(false, $"User {userId} not found.");
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return new ResponseResult(false,
                    result.Errors.Select(x => x.Description));
            }

            return new ResponseResult(true, $"User {userId} deleted.");
        }
    }
}
