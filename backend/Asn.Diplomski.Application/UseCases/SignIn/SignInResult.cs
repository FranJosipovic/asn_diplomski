namespace Asn.Diplomski.Application.UseCases.SignIn
{
    public class SignInResult
    {
        public long TenantId { get; set; }
        public string Email { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiresAt { get; set; }

        public bool IsUnauthorized { get; set; }

        public static SignInResult Success(long tenantId, string email, string accessToken, string refreshToken, DateTime refreshTokenExpiresAt)
            => new() { TenantId = tenantId, Email = email, AccessToken = accessToken, RefreshToken = refreshToken, RefreshTokenExpiresAt = refreshTokenExpiresAt };

        public static SignInResult Unauthorized()
            => new() { IsUnauthorized = true };
    }
}
