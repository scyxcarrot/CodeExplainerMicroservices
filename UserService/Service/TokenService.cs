using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using CodeExplainerCommon.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using UserService.DbContexts;
using UserService.Models;

namespace UserService.Service
{
    public class TokenService(
        IConfiguration configuration, 
        UserManager<AppUser> userManager,
        IDbContextFactory<UserDbContext> dbContextFactory) : ITokenService
    {
        private readonly SymmetricSecurityKey _symmetricSecurityKey =
            new(Encoding.UTF8.GetBytes(configuration["JWT:SigningKey"]!));

        public async Task<string> CreateToken(AppUser user)
        {
            
            var claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.NameId, user.Id),
            };

            var roles = await userManager.GetRolesAsync(user);
            claims.AddRange(
                roles.Select(
                    role => new Claim(ClaimTypes.Role, role)));

            var credentials = new SigningCredentials(_symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Expires = DateTime.UtcNow.AddSeconds(Token.AccessTokenExpiryTime),
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = credentials,
                Issuer = configuration["JWT:Issuer"],
                Audience = configuration["JWT:Audience"],
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<string> CreateRefreshToken(AppUser user)
        {
            var refreshToken = CreateRefreshToken();
            var userAuthentication = new UserToken()
            {
                RefreshToken = refreshToken,
                Id = Guid.NewGuid(),
                Invalidated = false,
                RefreshTokenExpiryDate = DateTime.UtcNow.AddSeconds(Token.RefreshTokenExpiryTime),
                UserId = user.Id,
            };

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await DeleteRefreshToken(user.Id);
            dbContext.UserTokens.Add(userAuthentication);
            await dbContext.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<string> GetUserIdFromRefreshToken(string refreshToken)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var foundUserAuthentication =
                await dbContext.UserTokens
                    .FirstOrDefaultAsync(userAuthentication => userAuthentication.RefreshToken == refreshToken);

            if (foundUserAuthentication != null &&
                !foundUserAuthentication.Invalidated &&
                foundUserAuthentication.RefreshTokenExpiryDate >= DateTime.UtcNow)
            {
                return foundUserAuthentication.UserId;
            }

            return "";
        }

        public async Task<bool> DeleteRefreshToken(string userId)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var foundUserAuthentication =
                await dbContext.UserTokens
                    .FirstOrDefaultAsync(
                        userAuthentication => userAuthentication.UserId == userId);

            if (foundUserAuthentication == null)
            {
                return false;
            }
            dbContext.UserTokens.Remove(foundUserAuthentication);
            await dbContext.SaveChangesAsync();

            return true;
        }

        private static string CreateRefreshToken()
        {
            // Create a byte array to hold the random bytes
            var randomBytes = new byte[32];

            // Fill the array with cryptographically strong random bytes
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Convert the byte array to a Base64 URL-encoded string.
            // Base64UrlEncode is preferred over standard Base64Encode for tokens
            // because it replaces URL-unsafe characters ('+' and '/') with URL-safe ones ('-' and '_')
            // and removes padding ('=') characters, making it suitable for URLs and headers.
            var base64String = Convert.ToBase64String(randomBytes);
            var base64UrlEncode = base64String.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return base64UrlEncode;
        }
    }
}
