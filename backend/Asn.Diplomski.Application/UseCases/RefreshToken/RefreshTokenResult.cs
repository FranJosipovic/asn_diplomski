namespace Asn.Diplomski.Application.UseCases.RefreshToken
{
    public class RefreshTokenResult
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiresAt { get; set; }

        public bool IsUnauthorized { get; set; }
        public bool IsExpired { get; set; }

        public static RefreshTokenResult Success(string accessToken, string refreshToken, DateTime expiresAt)
            => new() { AccessToken = accessToken, RefreshToken = refreshToken, RefreshTokenExpiresAt = expiresAt };

        public static RefreshTokenResult Unauthorized()
            => new() { IsUnauthorized = true };

        public static RefreshTokenResult Expired()
            => new() { IsExpired = true };
    }
}
