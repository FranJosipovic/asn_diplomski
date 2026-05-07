using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Asn.Diplomski.Application.UseCases.SignIn
{
    public class SignInHandler
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ITokenService _tokenService;

        public SignInHandler(ITenantRepository tenantRepository, ITokenService tokenService)
        {
            _tenantRepository = tenantRepository;
            _tokenService = tokenService;
        }

        public async Task<SignInResult> HandleAsync(SignInCommand command)
        {
            var tenant = await _tenantRepository.GetByEmailAsync(command.Email);
            if (tenant == null)
                return SignInResult.Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(command.Password, tenant.PasswordHash))
                return SignInResult.Unauthorized();

            var accessToken = _tokenService.GenerateAccessToken(tenant);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            tenant.RefreshToken = refreshToken;
            tenant.RefreshTokenExpiresAt = refreshTokenExpiresAt;
            await _tenantRepository.UpdateAsync(tenant);

            return SignInResult.Success(tenant.Id, tenant.Email, accessToken, refreshToken, refreshTokenExpiresAt);
        }
    }
}
