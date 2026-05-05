#include <WiFi.h>
#include <WiFiManager.h>
#include <PubSubClient.h>
#include "SHTC3-SOLDERED.h"

SHTC3 shtcSensor;
//x9ptbkxb5bxx2kxx
// ===== MQTT =====
const char* MQTT_HOST = "192.168.1.110";
const int   MQTT_PORT = 1883;

// ===== EASY TO CHANGE =====
const char* TENANT_ID = "1";
const char* DEVICE_ID = "1";
const char* SOIL_SENSOR_ID = "2";
const char* TEMP_SENSOR_ID = "1";

// ===== SOIL SENSOR CALIB =====
const int sensorPin = 10;
const int DRY_VALUE = 2800;
const int WET_VALUE = 0;

WiFiClient wifiClient;
PubSubClient mqtt(wifiClient);
WiFiManager wm;

void logLine(const String& msg)
{
    Serial.println("[LOG] " + msg);
}

void connectMqtt()
{
    logLine("Connecting to MQTT broker...");

    mqtt.setServer(MQTT_HOST, MQTT_PORT);

    while (!mqtt.connected()) {
        if (mqtt.connect("arduino-client", nullptr, nullptr, nullptr, 0, false, nullptr, true)) {
            logLine("MQTT connected");
        } else {
            logLine("MQTT failed. state=" + String(mqtt.state()));            
            delay(1000);
        }
    }
}

String topic(const char* sensor)
{
    const char* SENSOR_ID = "";

    if (strcmp(sensor, "temperature") == 0) {
        SENSOR_ID = TEMP_SENSOR_ID;
    } else if (strcmp(sensor, "soil") == 0) {
        SENSOR_ID = SOIL_SENSOR_ID;
    }

    return String("tenant_") + TENANT_ID +
           "/device_" + DEVICE_ID +
           "/sensor_" + SENSOR_ID + "/" + sensor;
}

void publishJson(const String& t, const String& payload)
{
    bool ok = mqtt.publish(t.c_str(), payload.c_str(), true);

    logLine("Publish → Topic: " + t);
    logLine("Payload: " + payload);
    logLine(String("Status: ") + (ok ? "OK" : "FAILED"));
}

String isoTimestamp()
{
    unsigned long s = millis() / 1000;
    return String("1970-01-01T00:00:") + String(s) + "Z";
}

String temperaturePayload(float temp)
{
    return String("{") +
        "\"value\":" + String(temp, 2) + "," +
        "\"unit\":\"°C\"," +
        "\"timestamp\":\"" + isoTimestamp() + "\"" +
    "}";
}

String soilPayload(int soilPct)
{
    return String("{") +
        "\"value\":" + String(soilPct) + "," +
        "\"unit\":\"%\"," +
        "\"timestamp\":\"" + isoTimestamp() + "\"" +
    "}";
}

void setup()
{
    Serial.begin(115200);
    delay(1000);
    wm.resetSettings();
    logLine("Booting device...");
    shtcSensor.begin();

    bool res = wm.autoConnect("Irrigation-Setup");

    if (!res) {
        ESP.restart();
    }

    logLine("WiFi connected. IP: " + WiFi.localIP().toString());

    connectMqtt();
}

void loop()
{
    if (!mqtt.connected())
        connectMqtt();

    mqtt.loop();

    shtcSensor.sample();

    float temp = shtcSensor.readTempC();
    int raw = analogRead(sensorPin);

    int soilPct = map(raw, DRY_VALUE, WET_VALUE, 0, 100);
    soilPct = constrain(soilPct, 0, 100);

    logLine("Temp: " + String(temp, 2) + " °C");
    logLine("Soil raw: " + String(raw));
    logLine("Soil moisture: " + String(soilPct) + " %");

    publishJson(topic("temperature"), temperaturePayload(temp));
    publishJson(topic("soil"), soilPayload(soilPct));

    logLine("Cycle complete\n");

    delay(5000);
}