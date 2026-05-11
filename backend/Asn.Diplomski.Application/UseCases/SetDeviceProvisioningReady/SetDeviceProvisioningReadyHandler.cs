using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities.Enums;
using System.Security.Cryptography;

namespace Asn.Diplomski.Application.UseCases.SetDeviceProvisioningReady
{
    public class SetDeviceProvisioningReadyHandler
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IMqttSubscriber _mqttSubscriber;

        public SetDeviceProvisioningReadyHandler(IDeviceRepository deviceRepository, IMqttSubscriber mqttSubscriber)
        {
            _deviceRepository = deviceRepository;
            _mqttSubscriber = mqttSubscriber;
        }

        public async Task<SetDeviceProvisioningReadyResult> HandleAsync(SetDeviceProvisioningReadyCommand command)
        {
            var device = await _deviceRepository.GetByIdAndTenantAsync(command.DeviceId, command.TenantId);
            if (device == null)
                return SetDeviceProvisioningReadyResult.NotFound();

            await _mqttSubscriber.UnsubscribeFromDeviceAsync(command.TenantId, command.DeviceId);

            var token = GenerateProvisioningToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            device.ProvisioningToken = token;
            device.ProvisioningTokenExpiresAt = expiresAt;
            device.Status = DeviceStatus.ProvisioningReady;

            await _deviceRepository.UpdateAsync(device);

            return SetDeviceProvisioningReadyResult.Success(token, expiresAt);
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
