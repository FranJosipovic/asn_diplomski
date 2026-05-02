using System.Threading.Channels;

namespace Asn.Diplomski.Server.Mqtt
{
    public class MqttIncomingChannel
    {
        private readonly Channel<MqttIncomingMessage> _channel =
            Channel.CreateUnbounded<MqttIncomingMessage>();

        public ChannelWriter<MqttIncomingMessage> Writer => _channel.Writer;
        public ChannelReader<MqttIncomingMessage> Reader => _channel.Reader;
    }
}
