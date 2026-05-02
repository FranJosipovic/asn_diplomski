using System.Threading.Channels;

namespace Asn.Diplomski.Server.Mqtt
{
    public class MqttOutgoingChannel
    {
        private readonly Channel<MqttOutgoingMessage> _channel =
            Channel.CreateUnbounded<MqttOutgoingMessage>();

        public ChannelWriter<MqttOutgoingMessage> Writer => _channel.Writer;
        public ChannelReader<MqttOutgoingMessage> Reader => _channel.Reader;
    }
}
