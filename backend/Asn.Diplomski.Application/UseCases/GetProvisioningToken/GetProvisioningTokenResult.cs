using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Application.UseCases.GetProvisioningToken
{
    public class GetProvisioningTokenResult
    {
        public List<DeviceProvisioningInfo> Devices { get; set; } = [];
        public DateTime ExpiresAt { get; set; }
        public bool IsNotFound { get; set; }

        public static GetProvisioningTokenResult Success(List<DeviceProvisioningInfo> devices, DateTime expiresAt)
            => new() { Devices = devices, ExpiresAt = expiresAt };

        public static GetProvisioningTokenResult NotFound()
            => new() { IsNotFound = true };
    }

    public class DeviceProvisioningInfo
    {
        public long DeviceId { get; set; }
        public DeviceType DeviceType { get; set; }
        public string DeviceSsid { get; set; } = null!;
        public ProvisionStatus ProvisionStatus { get; set; }
        public string ProvisioningToken { get; set; } = null!;
        public List<SensorProvisioningInfo> Sensors { get; set; } = [];
    }

    public class SensorProvisioningInfo
    {
        public long SensorId { get; set; }
        public SensorType SensorType { get; set; }
    }
}
