using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.HandleDeviceStatus
{
    public class HandleDeviceStatusHandler
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<HandleDeviceStatusHandler> _logger;

        public HandleDeviceStatusHandler(IDeviceRepository deviceRepository, ILogger<HandleDeviceStatusHandler> logger)
        {
            _deviceRepository = deviceRepository;
            _logger = logger;
        }

        public async Task HandleAsync(HandleDeviceStatusCommand command)
        {
            var device = await _deviceRepository.GetByIdAndTenantAsync(command.DeviceId, command.TenantId);
            if (device == null)
            {
                _logger.LogWarning("Status event '{Event}' za nepoznati uređaj {DeviceId} (tenant {TenantId})",
                    command.Event, command.DeviceId, command.TenantId);
                return;
            }

            var newStatus = command.Event switch
            {
                "started" => DeviceStatus.Working,
                "stopped" => DeviceStatus.Stopped,
                _ => (DeviceStatus?)null
            };

            if (newStatus == null)
            {
                _logger.LogWarning("Nepoznati status event '{Event}' za uređaj {DeviceId}", command.Event, command.DeviceId);
                return;
            }

            device.Status = newStatus.Value;
            device.LastSeenAt = DateTime.UtcNow;

            await _deviceRepository.UpdateAsync(device);

            _logger.LogInformation("Uređaj {DeviceId} (tenant {TenantId}) prešao u stanje {Status}",
                command.DeviceId, command.TenantId, newStatus);
        }
    }
}
