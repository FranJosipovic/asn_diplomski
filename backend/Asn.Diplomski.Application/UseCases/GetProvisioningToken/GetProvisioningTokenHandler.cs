using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities.Enums;
using System.Security.Cryptography;

namespace Asn.Diplomski.Application.UseCases.GetProvisioningToken
{
    public class GetProvisioningTokenHandler
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IDeviceRepository _deviceRepository;

        public GetProvisioningTokenHandler(ITenantRepository tenantRepository, IDeviceRepository deviceRepository)
        {
            _tenantRepository = tenantRepository;
            _deviceRepository = deviceRepository;
        }

        public async Task<GetProvisioningTokenResult> HandleAsync(GetProvisioningTokenCommand command)
        {
            var tenant = await _tenantRepository.GetByIdWithDevicesAndSensorsAsync(command.TenantId);
            if (tenant == null)
                return GetProvisioningTokenResult.NotFound();

            var expiresAt = DateTime.UtcNow.AddMinutes(15);
            var deviceInfos = new List<DeviceProvisioningInfo>();

            foreach (var device in tenant.Devices.Where(d => d.IsActive))
            {
                device.ProvisioningToken = GenerateProvisioningToken();
                device.ProvisioningTokenExpiresAt = expiresAt;
                device.ProvisionStatus = ProvisionStatus.Provisioning;

                deviceInfos.Add(new DeviceProvisioningInfo
                {
                    DeviceId = device.Id,
                    DeviceType = device.Type,
                    DeviceSsid = $"{device.Type}_{device.Id}",
                    ProvisionStatus = device.ProvisionStatus,
                    ProvisioningToken = device.ProvisioningToken,
                    Sensors = device.Sensors
                        .Where(s => s.IsActive)
                        .Select(s => new SensorProvisioningInfo
                        {
                            SensorId = s.Id,
                            SensorType = s.Type
                        })
                        .ToList()
                });
            }

            await _deviceRepository.UpdateRangeAsync(tenant.Devices.Where(d => d.IsActive).ToList());

            return GetProvisioningTokenResult.Success(deviceInfos, expiresAt);
        }

        private static string GenerateProvisioningToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
