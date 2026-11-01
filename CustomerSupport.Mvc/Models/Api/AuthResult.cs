namespace CustomerSupport.Mvc.Models.Api
{
    public sealed class AuthResult
    {
        public UserProfile User { get; set; } = new();
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
