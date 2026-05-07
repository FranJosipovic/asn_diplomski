namespace Asn.Diplomski.Application.UseCases.ConfirmMqttConnection
{
    public record ConfirmMqttConnectionCommand(long TenantId, long DeviceId);
}
