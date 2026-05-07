using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Application.UseCases.CompleteDeviceProvisioning
{
    public class CompleteDeviceProvisioningResult
    {
        public long TenantId { get; set; }
        public long DeviceId { get; set; }
        public List<ProvisionedSensorInfo> Sensors { get; set; } = [];

        public bool IsNotFound { get; set; }
        public bool IsExpired { get; set; }

        public static CompleteDeviceProvisioningResult Success(long tenantId, long deviceId, List<ProvisionedSensorInfo> sensors)
            => new() { TenantId = tenantId, DeviceId = deviceId, Sensors = sensors };

        public static CompleteDeviceProvisioningResult NotFound()
            => new() { IsNotFound = true };

        public static CompleteDeviceProvisioningResult Expired()
            => new() { IsExpired = true };
    }

    public class ProvisionedSensorInfo
    {
        public long SensorId { get; set; }
        public SensorType SensorType { get; set; }
    }
}
