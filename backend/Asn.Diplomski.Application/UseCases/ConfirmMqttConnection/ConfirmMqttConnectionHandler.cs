using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Application.UseCases.ConfirmMqttConnection
{
    public class ConfirmMqttConnectionHandler
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IMqttSubscriber _mqttSubscriber;

        public ConfirmMqttConnectionHandler(IDeviceRepository deviceRepository, IMqttSubscriber mqttSubscriber)
        {
            _deviceRepository = deviceRepository;
            _mqttSubscriber = mqttSubscriber;
        }

        public async Task<ConfirmMqttConnectionResult> HandleAsync(ConfirmMqttConnectionCommand command)
        {
            var device = await _deviceRepository.GetByIdAndTenantWithSensorsAsync(command.DeviceId, command.TenantId);

            if (device == null)
                return ConfirmMqttConnectionResult.NotFound();

            if (device.Status != DeviceStatus.Provisioning)
                return ConfirmMqttConnectionResult.InvalidState();

            device.Status = DeviceStatus.Ready;
            device.LastSeenAt = DateTime.UtcNow;

            await _deviceRepository.UpdateAsync(device);
            await _mqttSubscriber.SubscribeToDeviceAsync(device);

            return ConfirmMqttConnectionResult.Success();
        }
    }
}
