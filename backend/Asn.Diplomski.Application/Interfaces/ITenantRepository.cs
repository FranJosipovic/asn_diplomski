using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.Interfaces
{
    public interface ITenantRepository
    {
        Task<bool> HasAnyAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<Tenant?> GetByIdWithDevicesAndSensorsAsync(long id);
        Task<Tenant?> GetByEmailAsync(string email);
        Task<Tenant?> GetByRefreshTokenAsync(string refreshToken);
        Task AddAsync(Tenant tenant);
        Task UpdateAsync(Tenant tenant);
    }
}
