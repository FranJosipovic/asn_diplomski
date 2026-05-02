using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.Interfaces
{
    public interface IDeviceRepository
    {
        Task<Device?> GetByIdWithSensorsAsync(long id);
        Task<IReadOnlyList<Device>> GetAllActiveWithSensorsAsync();
        Task AddAsync(Device device);
        Task UpdateAsync(Device device);
    }
}
