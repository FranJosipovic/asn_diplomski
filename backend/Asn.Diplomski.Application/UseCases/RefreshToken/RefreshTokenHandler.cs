using Asn.Diplomski.Application.Interfaces;

namespace Asn.Diplomski.Application.UseCases.RefreshToken
{
    public class RefreshTokenHandler
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;

        public RefreshTokenHandler(ITenantRepository tenantRepository, ITokenService tokenService)
        {
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
        }

        public async Task<RefreshTokenResult> HandleAsync(RefreshTokenCommand command)
        {
            var tenant = await _tenantRepository.GetByRefreshTokenAsync(command.RefreshToken);

            if (tenant == null)
                return RefreshTokenResult.Unauthorized();

            if (tenant.RefreshTokenExpiresAt == null || DateTime.UtcNow > tenant.RefreshTokenExpiresAt)
                return RefreshTokenResult.Expired();

            var accessToken = _tokenService.GenerateAccessToken(tenant);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            tenant.RefreshToken = newRefreshToken;
            tenant.RefreshTokenExpiresAt = newRefreshTokenExpiresAt;
            await _tenantRepository.UpdateAsync(tenant);

            return RefreshTokenResult.Success(accessToken, newRefreshToken, newRefreshTokenExpiresAt);
        }
    }
}
