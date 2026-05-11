using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities.Enums;
using System.Security.Cryptography;

namespace Asn.Diplomski.Application.UseCases.RequestReprovision
{
    public class RequestReprovisionHandler
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IMqttSubscriber _mqttSubscriber;

        public RequestReprovisionHandler(IDeviceRepository deviceRepository, IMqttSubscriber mqttSubscriber)
        {
            _deviceRepository = deviceRepository;
            _mqttSubscriber = mqttSubscriber;
        }

        public async Task<RequestReprovisionResult> HandleAsync(RequestReprovisionCommand command)
        {
            var device = await _deviceRepository.GetByIdAndTenantAsync(command.DeviceId, command.TenantId);
            if (device == null)
                return RequestReprovisionResult.NotFound();

            await _mqttSubscriber.UnsubscribeFromDeviceAsync(command.TenantId, command.DeviceId);

            device.ProvisioningToken = GenerateProvisioningToken();
            device.ProvisioningTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
            device.Status = DeviceStatus.ProvisioningReady;
            device.LastSeenAt = DateTime.UtcNow;

            await _deviceRepository.UpdateAsync(device);
            return RequestReprovisionResult.Success();
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
