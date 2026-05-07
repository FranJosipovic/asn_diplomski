using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Application.UseCases.CompleteDeviceProvisioning
{
    public class CompleteDeviceProvisioningHandler
    {
        private readonly IDeviceRepository _deviceRepository;

        public CompleteDeviceProvisioningHandler(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        public async Task<CompleteDeviceProvisioningResult> HandleAsync(CompleteDeviceProvisioningCommand command)
        {
            var device = await _deviceRepository.GetByProvisioningTokenAsync(command.ProvisioningToken);

            if (device == null)
            {
                // Distinguish between not found and expired by checking without expiry filter
                var expired = await _deviceRepository.GetByProvisioningTokenIgnoringExpiryAsync(command.ProvisioningToken);
                return expired != null
                    ? CompleteDeviceProvisioningResult.Expired()
                    : CompleteDeviceProvisioningResult.NotFound();
            }

            device.ProvisionStatus = ProvisionStatus.Provisioned;
            device.ProvisioningToken = null;
            device.ProvisioningTokenExpiresAt = null;

            await _deviceRepository.UpdateAsync(device);

            var sensors = device.Sensors
                .Where(s => s.IsActive)
                .Select(s => new ProvisionedSensorInfo
                {
                    SensorId = s.Id,
                    SensorType = s.Type
                })
                .ToList();

            return CompleteDeviceProvisioningResult.Success(device.TenantId, device.Id, sensors);
        }
    }
}
