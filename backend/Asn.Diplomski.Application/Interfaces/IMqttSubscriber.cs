using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.Interfaces
{
    public interface IMqttSubscriber
    {
        Task SubscribeToDeviceAsync(Device device);
    }
}
