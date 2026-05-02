using Asn.Diplomski.Application.UseCases.HandleSoilMoisture;
using Asn.Diplomski.Application.UseCases.HandleWaterLevel;

namespace Asn.Diplomski.Server.Mqtt
{
    public class MqttIncomingWorker : BackgroundService
    {
        private readonly MqttIncomingChannel _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MqttIncomingWorker> _logger;

        public MqttIncomingWorker(
            MqttIncomingChannel channel,
            IServiceScopeFactory scopeFactory,
            ILogger<MqttIncomingWorker> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var msg in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessAsync(msg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Greška pri obradi MQTT poruke — topic: '{Topic}'", msg.Topic);
                }
            }
        }

        private async Task ProcessAsync(MqttIncomingMessage msg)
        {
            var slug = msg.Topic[(msg.Topic.LastIndexOf('/') + 1)..];

            switch (slug)
            {
                case MqttTopics.SlugSoilMoisture:
                {
                    if (!MqttMessageMapper.TryMapToSoilMoisture(msg.Topic, msg.Payload, out var cmd))
                        break;
                    using var scope = _scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<HandleSoilMoistureHandler>().HandleAsync(cmd);
                    break;
                }
                case MqttTopics.SlugWaterLevel:
                {
                    if (!MqttMessageMapper.TryMapToWaterLevel(msg.Topic, msg.Payload, out var cmd))
                        break;
                    using var scope = _scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<HandleWaterLevelHandler>().HandleAsync(cmd);
                    break;
                }
                default:
                    _logger.LogDebug(
                        "MQTT poruka nije mapirana ni na jedan use case — topic: '{Topic}'", msg.Topic);
                    break;
            }
        }
    }
}
