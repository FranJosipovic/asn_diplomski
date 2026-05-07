using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.Interfaces
{
    public interface IDeviceRepository
    {
        Task<Device?> GetByIdWithSensorsAsync(long id);
        Task<IReadOnlyList<Device>> GetAllActiveWithSensorsAsync();
        Task AddAsync(Device device);
        Task UpdateAsync(Device device);
        Task UpdateRangeAsync(IList<Device> devices);
        Task<Device?> GetByProvisioningTokenAsync(string token);
        Task<Device?> GetByProvisioningTokenIgnoringExpiryAsync(string token);
        Task<Device?> GetByIdAndTenantAsync(long deviceId, long tenantId);
        Task<IReadOnlyList<Device>> GetAllProvisionedByTenantAsync(long tenantId);
    }
}
