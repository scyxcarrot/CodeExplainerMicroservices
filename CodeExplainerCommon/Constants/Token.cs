namespace CodeExplainerCommon.Constants
{
    public class Token
    {
        public const string AccessToken = "CodeExplainerAccessToken";
        public const string RefreshToken = "CodeExplainerRefreshToken";

        // expiry time is in seconds
        public const double AccessTokenExpiryTime = 5 * 60;
        public const double RefreshTokenExpiryTime = 7 * 24 * 60 * 60;
    }
}
