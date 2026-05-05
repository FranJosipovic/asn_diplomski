using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Application.Mqtt;
using Asn.Diplomski.Domain.Entities;
using Asn.Diplomski.Domain.Entities.Enums;
using MQTTnet;
using MQTTnet.Protocol;
using System.Collections.Concurrent;

namespace Asn.Diplomski.Server.Mqtt
{
    public class MqttService : IHostedService, IMqttSubscriber
    {
        private readonly IMqttClient _client;
        private readonly MqttClientOptions _options;
        private readonly ILogger<MqttService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MqttIncomingChannel _incomingChannel;
        private readonly string _brokerHost;
        private readonly int _brokerPort;

        private readonly ConcurrentDictionary<string, byte> _subscribedTopics = new();
        private bool _stopping;

        public MqttService(
            IConfiguration config,
            ILogger<MqttService> logger,
            IServiceScopeFactory scopeFactory,
            MqttIncomingChannel incomingChannel)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _incomingChannel = incomingChannel;

            _brokerHost = config["Mqtt:Host"] ?? "localhost";
            _brokerPort = config.GetValue<int>("Mqtt:Port", 1883);

            var factory = new MqttClientFactory();
            _client = factory.CreateMqttClient();

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(_brokerHost, _brokerPort)
                .WithClientId("asn-server")
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .Build();

            _client.ConnectedAsync += OnConnectedAsync;
            _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            _client.DisconnectedAsync += OnDisconnectedAsync;
        }

        // ── IHostedService ──────────────────────────────────────

        public async Task StartAsync(CancellationToken cancellationToken)
            => await ConnectAsync();

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopping = true;
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
                _logger.LogInformation("MQTT klijent uredno odspojio.");
            }
        }

        // ── IMqttSubscriber ─────────────────────────────────────

        public async Task SubscribeToDeviceAsync(Device device)
        {
            foreach (var sensor in device.Sensors.Where(s => s.IsActive && s.Type != SensorType.PumpCommand))
            {
                var topic = MqttTopics.BuildSensorTopic(device.TenantId, device.Id, sensor.Type);
                _subscribedTopics.TryAdd(topic, 0);

                if (!_client.IsConnected)
                {
                    _logger.LogWarning(
                        "Klijent nije spojen — topic '{Topic}' snimljen, pretplata će se aktivirati pri spajanju.", topic);
                    continue;
                }

                await SubscribeAsync(topic);
            }
        }

        // ── Public: used by MqttOutgoingWorker ──────────────────

        public async Task PublishAsync(string topic, string json)
        {
            if (!_client.IsConnected)
            {
                _logger.LogWarning("Publish nije moguć — MQTT klijent nije spojen.");
                return;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(message);

            _logger.LogInformation(
                "MQTT komanda objavljena — topic: '{Topic}' | payload: {Payload}", topic, json);
        }

        // ── Private ─────────────────────────────────────────────

        private async Task ConnectAsync()
        {
            try
            {
                await _client.ConnectAsync(_options, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Greška pri spajanju na MQTT broker {Host}:{Port}.", _brokerHost, _brokerPort);
            }
        }

        private async Task SubscribeAsync(string topic)
        {
            var result = await _client.SubscribeAsync(
                new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f
                        .WithTopic(topic)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce))
                    .Build(),
                CancellationToken.None);

            var code = result.Items.FirstOrDefault()?.ResultCode;
            _logger.LogInformation(
                "MQTT pretplata — topic: '{Topic}', broker ACK: {ResultCode}.", topic, code);
        }

        private async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation(
                "MQTT klijent spojen na broker {Host}:{Port}.", _brokerHost, _brokerPort);

            using var scope = _scopeFactory.CreateScope();
            var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();

            var devices = await deviceRepository.GetAllActiveWithSensorsAsync();
            foreach (var device in devices)
                await SubscribeToDeviceAsync(device);
        }

        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString() ?? string.Empty;

            _logger.LogInformation(
                "MQTT poruka — topic: '{Topic}' | payload: {Payload}", topic, payload);

            _incomingChannel.Writer.TryWrite(new MqttIncomingMessage(topic, payload));
            return Task.CompletedTask;
        }

        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            if (_stopping) return;

            _logger.LogWarning(
                "MQTT klijent odspojio. Ponovni pokušaj za 5s... (razlog: {Reason})",
                e.Reason);

            await Task.Delay(TimeSpan.FromSeconds(5));
            await ConnectAsync();
        }
    }
}
