namespace Asn.Diplomski.Server.Mqtt
{
    public class MqttOutgoingWorker : BackgroundService
    {
        private readonly MqttOutgoingChannel _channel;
        private readonly MqttService _mqttService;
        private readonly ILogger<MqttOutgoingWorker> _logger;

        public MqttOutgoingWorker(
            MqttOutgoingChannel channel,
            MqttService mqttService,
            ILogger<MqttOutgoingWorker> logger)
        {
            _channel = channel;
            _mqttService = mqttService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var msg in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await _mqttService.PublishAsync(msg.Topic, msg.Json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Greška pri publishu MQTT poruke — topic: '{Topic}'", msg.Topic);
                }
            }
        }
    }
}
