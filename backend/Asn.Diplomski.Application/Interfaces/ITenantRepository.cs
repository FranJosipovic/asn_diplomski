using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.Interfaces
{
    public interface ITenantRepository
    {
        Task<bool> HasAnyAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<Tenant?> GetByIdWithDevicesAndSensorsAsync(long id);
        Task AddAsync(Tenant tenant);
    }
}
