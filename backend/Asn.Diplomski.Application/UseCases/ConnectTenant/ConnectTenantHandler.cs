using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Application.Mqtt;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.ConnectTenant
{
    public class ConnectTenantHandler
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IMqttSubscriber _mqttSubscriber;
        private readonly ILogger<ConnectTenantHandler> _logger;

        public ConnectTenantHandler(
            IDeviceRepository deviceRepository,
            ITenantRepository tenantRepository,
            IMqttSubscriber mqttSubscriber,
            ILogger<ConnectTenantHandler> logger)
        {
            _deviceRepository = deviceRepository;
            _tenantRepository = tenantRepository;
            _mqttSubscriber = mqttSubscriber;
            _logger = logger;
        }

        public async Task<ConnectTenantResult> HandleAsync(long tenantId)
        {
            var tenant = await _tenantRepository.GetByIdWithDevicesAndSensorsAsync(tenantId);

            if (tenant is null)
            {
                _logger.LogWarning(
                    "Connect zahtjev odbijen — tenatn s ID-em {tenantId} ne postoji.", tenantId);
                return ConnectTenantResult.NotFound();
            }

            var devices = tenant.Devices.ToList();

            var topics = new List<string>();

            foreach (var item in devices)
            {
                item.LastSeenAt = DateTime.UtcNow;
                await _deviceRepository.UpdateAsync(item);

                _logger.LogInformation(
                    "Uređaj {DeviceId} (tenant {TenantId}) se spojio. Pokretanje MQTT pretplate...",
                    item.Id, item.TenantId);

                await _mqttSubscriber.SubscribeToDeviceAsync(item);

                foreach (var sensor in item.Sensors)
                {
                    topics.Add(MqttTopics.BuildSensorTopic(tenantId, item.Id, sensor.Id, sensor.Type));
                }
            }


            return ConnectTenantResult.Success(devices, topics);
        }
    }
}
