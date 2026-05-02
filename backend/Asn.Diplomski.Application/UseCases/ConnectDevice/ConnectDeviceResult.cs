using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.UseCases.ConnectDevice
{
    public class ConnectDeviceResult
    {
        public bool IsNotFound { get; init; }
        public Device? Device { get; init; }
        public string? Topic { get; init; }

        public static ConnectDeviceResult NotFound() => new() { IsNotFound = true };
        public static ConnectDeviceResult Success(Device device, string topic)
            => new() { Device = device, Topic = topic };
    }
}
