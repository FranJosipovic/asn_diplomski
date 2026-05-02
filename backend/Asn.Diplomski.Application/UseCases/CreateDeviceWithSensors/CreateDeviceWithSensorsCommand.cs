using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Application.UseCases.CreateDeviceWithSensors
{
    public record CreateDeviceWithSensorsCommand(
        long TenantId,
        DeviceType Type,
        int DeviceNumber,
        string? Description);
}
