using Asn.Diplomski.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.ConnectDevice
{
    public class ConnectDeviceHandler
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IMqttSubscriber _mqttSubscriber;
        private readonly ILogger<ConnectDeviceHandler> _logger;

        public ConnectDeviceHandler(
            IDeviceRepository deviceRepository,
            IMqttSubscriber mqttSubscriber,
            ILogger<ConnectDeviceHandler> logger)
        {
            _deviceRepository = deviceRepository;
            _mqttSubscriber = mqttSubscriber;
            _logger = logger;
        }

        public async Task<ConnectDeviceResult> HandleAsync(long deviceId)
        {
            var device = await _deviceRepository.GetByIdWithSensorsAsync(deviceId);

            if (device is null)
            {
                _logger.LogWarning(
                    "Connect zahtjev odbijen — uređaj s ID-em {DeviceId} ne postoji.", deviceId);
                return ConnectDeviceResult.NotFound();
            }

            device.LastSeenAt = DateTime.UtcNow;
            await _deviceRepository.UpdateAsync(device);

            _logger.LogInformation(
                "Uređaj {DeviceId} (tenant {TenantId}) se spojio. Pokretanje MQTT pretplate...",
                device.Id, device.TenantId);

            await _mqttSubscriber.SubscribeToDeviceAsync(device);

            var topic = $"tenant_{device.TenantId}/device_{device.Id}/sensor/+";
            return ConnectDeviceResult.Success(device, topic);
        }
    }
}
