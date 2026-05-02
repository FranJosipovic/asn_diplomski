using Asn.Diplomski.Application.Interfaces;
using System.Text.Json;

namespace Asn.Diplomski.Server.Mqtt
{
    public class MqttPublisher : IMqttPublisher
    {
        private readonly MqttOutgoingChannel _channel;
        private readonly ILogger<MqttPublisher> _logger;

        public MqttPublisher(MqttOutgoingChannel channel, ILogger<MqttPublisher> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        public void Enqueue(long tenantId, long deviceId, object payload)
        {
            var topic = $"tenant_{tenantId}/device_{deviceId}/command";
            var json = JsonSerializer.Serialize(payload);

            if (!_channel.Writer.TryWrite(new MqttOutgoingMessage(topic, json)))
                _logger.LogWarning(
                    "Nije moguće dodati poruku u outgoing channel — topic: '{Topic}'", topic);
        }
    }
}
