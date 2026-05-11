namespace Asn.Diplomski.Application.UseCases.HandleDeviceStatus
{
    public record HandleDeviceStatusCommand(long TenantId, long DeviceId, string Event);
}
