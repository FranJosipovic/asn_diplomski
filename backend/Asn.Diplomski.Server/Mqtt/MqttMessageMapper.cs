using Asn.Diplomski.Application.Mqtt;
using Asn.Diplomski.Application.UseCases.HandleDeviceStatus;
using Asn.Diplomski.Application.UseCases.HandleSoilMoisture;
using Asn.Diplomski.Application.UseCases.HandleTemperature;
using Asn.Diplomski.Application.UseCases.HandleWaterLevel;
using System.Text.Json;

namespace Asn.Diplomski.Server.Mqtt
{
    public static class MqttMessageMapper
    {
        public static bool TryMapToSoilMoisture(
            string topic,
            string payload,
            out HandleSoilMoistureCommand command)
        {
            command = default!;

            if (!MqttTopics.TryParseTenantDevice(topic, out var tenantId, out var deviceId, out var sensorId))
                return false;

            if (!TryParseValue(payload, out var value))
                return false;

            command = new HandleSoilMoistureCommand(tenantId, deviceId,sensorId, value);
            return true;
        }

        public static bool TryMapToWaterLevel(
            string topic,
            string payload,
            out HandleWaterLevelCommand command)
        {
            command = default!;

            if (!MqttTopics.TryParseTenantDevice(topic, out var tenantId, out var deviceId, out var sensorId))
                return false;

            if (!TryParseValue(payload, out var value))
                return false;

            command = new HandleWaterLevelCommand(tenantId, deviceId,sensorId, value);
            return true;
        }

        public static bool TryMapToTemperature(
            string topic,
            string payload,
            out HandleTemperatureCommand command)
        {
            command = default!;

            if (!MqttTopics.TryParseTenantDevice(topic, out var tenantId, out var deviceId, out var sensorId))
                return false;

            if (!TryParseValue(payload, out var value))
                return false;

            command = new HandleTemperatureCommand(tenantId, deviceId, sensorId, value);
            return true;
        }

        public static bool TryMapToDeviceStatus(
            string topic,
            string payload,
            out HandleDeviceStatusCommand command)
        {
            command = default!;

            if (!MqttTopics.TryParseStatusTopic(topic, out var tenantId, out var deviceId))
                return false;

            try
            {
                var doc = JsonDocument.Parse(payload);
                if (!doc.RootElement.TryGetProperty("event", out var prop))
                    return false;
                var eventName = prop.GetString();
                if (string.IsNullOrEmpty(eventName))
                    return false;
                command = new HandleDeviceStatusCommand(tenantId, deviceId, eventName);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryParseValue(string payload, out double value)
        {
            value = 0;
            try
            {
                var doc = JsonDocument.Parse(payload);
                if (!doc.RootElement.TryGetProperty("value", out var prop))
                    return false;
                value = prop.GetDouble();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
