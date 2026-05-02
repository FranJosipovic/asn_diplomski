namespace Asn.Diplomski.Application.Interfaces
{
    public interface IMqttPublisher
    {
        void Enqueue(long tenantId, long deviceId, object payload);
    }
}
