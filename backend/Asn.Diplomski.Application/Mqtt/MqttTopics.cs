using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Application.Mqtt
{
    public static class MqttTopics
    {
        public const string SlugSoilMoisture = "soil";
        public const string SlugWaterLevel   = "water-level";
        public const string SlugTemperature  = "temperature";
        public const string SlugStatus       = "status";

        public static string BuildSensorTopic(long tenantId, long deviceId, long sensorId, SensorType sensorType)
            => $"tenant_{tenantId}/device_{deviceId}/sensor_{sensorId}/{GetSensorSlug(sensorType)}";

        public static string BuildStatusTopic(long tenantId, long deviceId)
            => $"tenant_{tenantId}/device_{deviceId}/status";

        public static bool TryParseStatusTopic(string topic, out long tenantId, out long deviceId)
        {
            tenantId = 0;
            deviceId = 0;
            // "tenant_1/device_2/status"
            var parts = topic.Split('/');
            if (parts.Length < 3) return false;
            return long.TryParse(parts[0].Replace("tenant_", ""), out tenantId)
                && long.TryParse(parts[1].Replace("device_", ""), out deviceId);
        }

        public static bool TryParseTenantDevice(string topic, out long tenantId, out long deviceId, out long sensorId)
        {
            tenantId = 0;
            deviceId = 0;
            sensorId = 0;
            // "tenant_1/device_2/sensor_1/soil"
            var parts = topic.Split('/');
            if (parts.Length < 2) return false;
            return long.TryParse(parts[0].Replace("tenant_", ""), out tenantId)
                && long.TryParse(parts[1].Replace("device_", ""), out deviceId)
                && long.TryParse(parts[2].Replace("sensor_", ""), out sensorId);
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
