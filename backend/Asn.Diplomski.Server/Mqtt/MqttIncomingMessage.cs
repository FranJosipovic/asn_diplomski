namespace Asn.Diplomski.Server.Mqtt
{
    public record MqttIncomingMessage(string Topic, string Payload);
}
