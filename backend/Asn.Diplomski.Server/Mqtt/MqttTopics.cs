using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Server.Mqtt
{
    internal static class MqttTopics
    {
        public const string SlugSoilMoisture = "soil";
        public const string SlugWaterLevel   = "water-level";
        public const string SlugTemperature  = "temperature";

        public static string BuildSensorTopic(long tenantId, long deviceId, SensorType sensorType)
            => $"tenant_{tenantId}/device_{deviceId}/sensor/{GetSensorSlug(sensorType)}";

        public static bool TryParseTenantDevice(string topic, out long tenantId, out long deviceId)
        {
            tenantId = 0;
            deviceId = 0;
            // "tenant_1/device_2/sensor/soil"
            var parts = topic.Split('/');
            if (parts.Length < 2) return false;
            return long.TryParse(parts[0].Replace("tenant_", ""), out tenantId)
                && long.TryParse(parts[1].Replace("device_", ""), out deviceId);
        }

        public static string GetSensorSlug(SensorType type) => type switch
        {
            SensorType.Temperature  => SlugTemperature,
            SensorType.SoilMoisture => SlugSoilMoisture,
            SensorType.WaterLevel   => SlugWaterLevel,
            _                       => type.ToString().ToLower()
        };
    }
}
