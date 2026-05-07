using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Application.UseCases.ConfirmMqttConnection
{
    public class ConfirmMqttConnectionHandler
    {
        private readonly IDeviceRepository _deviceRepository;

        public ConfirmMqttConnectionHandler(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        public async Task<ConfirmMqttConnectionResult> HandleAsync(ConfirmMqttConnectionCommand command)
        {
            var device = await _deviceRepository.GetByIdAndTenantAsync(command.DeviceId, command.TenantId);

            if (device == null)
                return ConfirmMqttConnectionResult.NotFound();

            if (device.ProvisionStatus != ProvisionStatus.Provisioning)
                return ConfirmMqttConnectionResult.InvalidState();

            device.ProvisionStatus = ProvisionStatus.Provisioned;
            device.LastSeenAt = DateTime.UtcNow;

            await _deviceRepository.UpdateAsync(device);

            return ConfirmMqttConnectionResult.Success();
        }
    }
}
