using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(Tenant tenant);
        string GenerateRefreshToken();
    }
}
