//x9ptbkxb5bxx2kxx
#include <WiFi.h>
#include <PubSubClient.h>
#include "SHTC3-SOLDERED.h"
#include <network_provisioning/manager.h>
#include <network_provisioning/scheme_softap.h>
#include <Preferences.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <Adafruit_NeoPixel.h>

// ─── LED ──────────────────────────────────────────────────────────────────────
#define LED_PIN    2
#define LED_COUNT  1

Adafruit_NeoPixel led(LED_COUNT, LED_PIN, NEO_GRB + NEO_KHZ800);

enum LedState {
  LED_NOT_PROVISIONED,  // solid orange  — no WiFi credentials stored
  LED_PROVISIONING,     // blink purple  — SoftAP active, waiting for app
  LED_BACKEND_CONFIG,   // blink blue    — WiFi ok, talking to backend / MQTT connecting
  LED_READY,            // solid blue    — MQTT connected, waiting for start command
  LED_WORKING,          // blink green   — sending sensor data
  LED_STOPPED           // solid red     — received stop command
};

LedState ledState = LED_NOT_PROVISIONED;
unsigned long ledTimer = 0;
bool ledOn = false;

void setColor(uint8_t r, uint8_t g, uint8_t b) {
  led.setPixelColor(0, led.Color(r, g, b));
  led.show();
}

void updateLed() {
  unsigned long now = millis();
  switch (ledState) {
    case LED_NOT_PROVISIONED:
      setColor(255, 80, 0);
      break;

    case LED_PROVISIONING:
      if (now - ledTimer > 400) {
        ledTimer = now;
        ledOn = !ledOn;
        setColor(ledOn ? 180 : 0, 0, ledOn ? 255 : 0);  // blink purple
      }
      break;

    case LED_BACKEND_CONFIG:
      if (now - ledTimer > 400) {
        ledTimer = now;
        ledOn = !ledOn;
        setColor(0, 0, ledOn ? 255 : 0);  // blink blue
      }
      break;

    case LED_READY:
      setColor(0, 0, 255);  // solid blue
      break;

    case LED_WORKING:
      if (now - ledTimer > 800) {
        ledTimer = now;
        ledOn = !ledOn;
        setColor(0, ledOn ? 255 : 0, 0);  // blink green
      }
      break;

    case LED_STOPPED:
      setColor(255, 0, 0);  // solid red
      break;
  }
}

// ─── Device identity ──────────────────────────────────────────────────────────
#define PROV_SSID "SensorUnit_21"
#define RESET_PIN 0

// ─── Soil sensor calibration ──────────────────────────────────────────────────
const int sensorPin = 10;
const int DRY_VALUE = 2800;
const int WET_VALUE = 0;

// ─── Runtime state ────────────────────────────────────────────────────────────
bool wifiReady = false;
bool systemStarted = false;
bool mqttConfirmSent = false;

unsigned long buttonPressedAt = 0;

String mqttHost = "";
int    mqttPort  = 1883;
String srvHost   = "";
int    srvPort   = 80;
String tenantId  = "";
String deviceId  = "";
String tempSensorId = "";
String soilSensorId = "";

// ─── Peripherals ──────────────────────────────────────────────────────────────
WiFiClient   wifiClient;
PubSubClient mqtt(wifiClient);
Preferences  prefs;
SHTC3        shtcSensor;

// ─── Helpers ──────────────────────────────────────────────────────────────────
void logLine(const String& msg) { Serial.println("[LOG] " + msg); }

String topic(const String& sensorId, const char* slug) {
  return String("tenant_") + tenantId + "/device_" + deviceId + "/sensor_" + sensorId + "/" + slug;
}

String commandTopic() {
  return String("tenant_") + tenantId + "/device_" + deviceId + "/command";
}

String statusTopic() {
  return String("tenant_") + tenantId + "/device_" + deviceId + "/status";
}

void publishJson(const String& t, const String& payload) {
  bool ok = mqtt.publish(t.c_str(), payload.c_str(), true);
  logLine("Publish → Topic: " + t);
  logLine("Payload: " + payload);
  logLine(String("Status: ") + (ok ? "OK" : "FAILED"));
}

String isoTimestamp() {
  unsigned long s = millis() / 1000;
  return String("1970-01-01T00:00:") + String(s) + "Z";
}

String temperaturePayload(float temp) {
  return String("{")
    + "\"value\":"       + String(temp, 2) + ","
    + "\"unit\":\"°C\","
    + "\"timestamp\":\"" + isoTimestamp() + "\""
    + "}";
}

String soilPayload(int soilPct) {
  return String("{")
    + "\"value\":"       + String(soilPct) + ","
    + "\"unit\":\"%\","
    + "\"timestamp\":\"" + isoTimestamp() + "\""
    + "}";
}

// ─── MQTT message callback ────────────────────────────────────────────────────
void onMqttMessage(char* topic, byte* payload, unsigned int length) {
  String msg((char*)payload, length);
  logLine("MQTT message on " + String(topic) + ": " + msg);

  JsonDocument doc;
  if (deserializeJson(doc, msg) != DeserializationError::Ok) return;

  String cmd = doc["command"].as<String>();
  if (cmd == "start") {
    logLine("Received 'start' command — beginning sensor loop.");
    systemStarted = true;
    ledState = LED_WORKING;
    publishJson(statusTopic(), "{\"event\":\"started\"}");
  } else if (cmd == "stop") {
    logLine("Received 'stop' command — halting sensor loop.");
    systemStarted = false;
    ledState = LED_STOPPED;
    publishJson(statusTopic(), "{\"event\":\"stopped\"}");
  }
}

// ─── Confirm MQTT connection to backend ──────────────────────────────────────
void confirmMqttConnection() {
  if (srvHost.isEmpty()) return;
  String url  = "http://" + srvHost + ":" + String(srvPort) + "/api/provision/confirm";
  String body = "{\"tenantId\":" + tenantId + ",\"deviceId\":" + deviceId + "}";
  logLine("Confirming MQTT connection: POST " + url);

  HTTPClient http;
  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  int code = http.POST(body);
  logLine("Confirm response: " + String(code));
  http.end();
}

// ─── Full reprovision: notify backend then wipe everything ───────────────────
void requestReprovision() {
  if (srvHost.isEmpty() || tenantId.isEmpty() || deviceId.isEmpty()) {
    logLine("Cannot reprovision — srvHost/tenantId/deviceId not set");
    return;
  }

  ledState = LED_NOT_PROVISIONED;  // solid orange while notifying backend
  updateLed();

  String url  = "http://" + srvHost + ":" + String(srvPort) + "/api/provision/reprovision";
  String body = "{\"tenantId\":" + tenantId + ",\"deviceId\":" + deviceId + "}";
  logLine("Notifying backend of reprovision: POST " + url);

  HTTPClient http;
  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  int code = http.POST(body);
  logLine("Reprovision notify response: " + String(code));
  http.end();

  // Wipe all stored config regardless of backend response — device is going to SoftAP
  WiFi.disconnect(true, true);
  prefs.begin("prov", false); prefs.clear(); prefs.end();
  prefs.begin("mqtt", false); prefs.clear(); prefs.end();
  logLine("All data wiped — restarting into provisioning mode...");
  delay(500);
  ESP.restart();
}

// ─── Button hold detection (5 s) ─────────────────────────────────────────────
void checkReprovisionButton() {
  if (tenantId.isEmpty() || deviceId.isEmpty()) return;  // not yet provisioned

  if (digitalRead(RESET_PIN) == LOW) {
    if (buttonPressedAt == 0) {
      buttonPressedAt = millis();
      logLine("Button held — keep holding 5 s to reprovision");
    } else if (millis() - buttonPressedAt >= 5000) {
      buttonPressedAt = 0;
      logLine("5 s hold detected — reprovisioning...");
      requestReprovision();
    }
  } else {
    buttonPressedAt = 0;
  }
}

// ─── MQTT connect + subscribe ─────────────────────────────────────────────────
unsigned long mqttRetryAt = 0;

void connectMqtt() {
  if (mqttHost.isEmpty()) {
    logLine("mqttHost not set — skipping MQTT connect");
    return;
  }

  if (millis() < mqttRetryAt) return;  // not time to retry yet

  mqtt.setServer(mqttHost.c_str(), mqttPort);
  mqtt.setCallback(onMqttMessage);
  mqtt.setKeepAlive(60);
  mqtt.setSocketTimeout(5);

  logLine("Connecting to MQTT at " + mqttHost + ":" + String(mqttPort) + "...");

  if (mqtt.connect("arduino-client", nullptr, nullptr, nullptr, 0, false, nullptr, true)) {
    logLine("MQTT connected");
    mqtt.subscribe(commandTopic().c_str());
    logLine("Subscribed to: " + commandTopic());

    if (!mqttConfirmSent) {
      confirmMqttConnection();
      mqttConfirmSent = true;
    }

    ledState = LED_READY;  // solid blue — waiting for start command
  } else {
    logLine("MQTT failed, state=" + String(mqtt.state()) + " — retrying in 5 s");
    mqttRetryAt = millis() + 5000;
  }
}

// ─── Complete provisioning: POST token to backend ────────────────────────────
void completeProvisioning() {
  ledState = LED_BACKEND_CONFIG;  // blink blue while talking to backend

  prefs.begin("prov", true);
  String token = prefs.getString("token", "");
  String host  = prefs.getString("host",  "");
  int    port  = prefs.getInt   ("port",  0);
  prefs.end();

  if (token.isEmpty() || host.isEmpty()) {
    logLine("No provisioning token — loading saved config.");

    prefs.begin("mqtt", true);
    mqttHost     = prefs.getString("host",    "");
    mqttPort     = prefs.getInt   ("port",    1883);
    srvHost      = prefs.getString("srvHost", "");
    srvPort      = prefs.getInt   ("srvPort", 80);
    tenantId     = prefs.getString("tenant",  "");
    deviceId     = prefs.getString("device",  "");
    tempSensorId = prefs.getString("tempId",  "");
    soilSensorId = prefs.getString("soilId",  "");
    prefs.end();

    logLine("Loaded — MQTT=" + mqttHost + ":" + String(mqttPort)
            + " tenant=" + tenantId + " device=" + deviceId);

    mqttConfirmSent = true;  // already confirmed on first boot
    return;
  }

  String url  = "http://" + host + ":" + String(port) + "/api/provision";
  String body = "{\"token\":\"" + token + "\"}";
  logLine("Completing provisioning: POST " + url);

  HTTPClient http;
  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  int code = http.POST(body);
  logLine("Provisioning response: " + String(code));

  if (code == 200) {
    String respBody = http.getString();
    logLine("Body: " + respBody);

    JsonDocument doc;
    if (deserializeJson(doc, respBody) == DeserializationError::Ok) {
      mqttHost = doc["mqttHost"].as<String>();
      mqttPort = doc["mqttPort"].as<int>();
      tenantId = String(doc["tenantId"].as<long>());
      deviceId = String(doc["deviceId"].as<long>());
      srvHost  = host;
      srvPort  = port;

      for (JsonObject sensor : doc["sensors"].as<JsonArray>()) {
        String type = sensor["sensorType"].as<String>();
        String id   = String(sensor["sensorId"].as<long>());
        if (type == "Temperature")  tempSensorId = id;
        if (type == "SoilMoisture") soilSensorId = id;
      }

      prefs.begin("mqtt", false);
      prefs.putString("host",    mqttHost);
      prefs.putInt   ("port",    mqttPort);
      prefs.putString("srvHost", srvHost);
      prefs.putInt   ("srvPort", srvPort);
      prefs.putString("tenant",  tenantId);
      prefs.putString("device",  deviceId);
      prefs.putString("tempId",  tempSensorId);
      prefs.putString("soilId",  soilSensorId);
      prefs.end();

      prefs.begin("prov", false);
      prefs.remove("token");
      prefs.end();

      logLine("Done. Tenant=" + tenantId + " Device=" + deviceId
              + " MQTT=" + mqttHost + ":" + String(mqttPort));
      logLine("TempSensor=" + tempSensorId + " SoilSensor=" + soilSensorId);
    }
  }

  http.end();
}

// ─── Custom provisioning endpoint ────────────────────────────────────────────
static esp_err_t provDataHandler(uint32_t session_id,
                                 const uint8_t *inbuf, ssize_t inlen,
                                 uint8_t **outbuf, ssize_t *outlen,
                                 void *priv_data) {
  if (inbuf && inlen > 0) {
    String json((const char *)inbuf, inlen);
    Serial.printf("[PROV] Received: %s\n", json.c_str());

    JsonDocument doc;
    if (deserializeJson(doc, json) == DeserializationError::Ok) {
      prefs.begin("prov", false);
      prefs.putString("token", doc["token"].as<String>());
      prefs.putString("host",  doc["host"].as<String>());
      prefs.putInt   ("port",  doc["port"].as<int>());
      prefs.end();
      Serial.println("[PROV] Data saved.");
    }
  }

  const char* resp = "{\"status\":\"ok\"}";
  size_t len = strlen(resp);
  *outbuf = (uint8_t*) malloc(len);
  if (*outbuf == NULL) { *outlen = 0; return ESP_ERR_NO_MEM; }
  memcpy(*outbuf, resp, len);
  *outlen = len;
  return ESP_OK;
}

// ─── WiFi / provisioning events ──────────────────────────────────────────────
void onEvent(arduino_event_t *event) {
  switch (event->event_id) {

    case ARDUINO_EVENT_PROV_START:
      logLine("Provisioning AP started — connect phone to: " PROV_SSID);
      ledState = LED_PROVISIONING;  // blink purple
      break;

    case ARDUINO_EVENT_PROV_CRED_RECV:
      logLine("Provisioning: WiFi credentials received.");
      break;

    case ARDUINO_EVENT_PROV_CRED_SUCCESS:
      logLine("Provisioning: credentials accepted.");
      break;

    case ARDUINO_EVENT_PROV_CRED_FAIL:
      logLine("Provisioning: FAILED — bad credentials.");
      ledState = LED_NOT_PROVISIONED;  // back to solid orange
      WiFi.disconnect(true, true);
      break;

    case ARDUINO_EVENT_PROV_END:
      logLine("Provisioning session ended.");
      break;

    case ARDUINO_EVENT_WIFI_STA_GOT_IP:
      logLine("WiFi connected. IP: "
              + IPAddress(event->event_info.got_ip.ip_info.ip.addr).toString());
      wifiReady = true;
      ledState = LED_BACKEND_CONFIG;  // blink blue — calling backend
      completeProvisioning();
      break;

    case ARDUINO_EVENT_WIFI_STA_DISCONNECTED:
      logLine("WiFi disconnected — reconnecting...");
      wifiReady = false;
      WiFi.begin();
      break;

    default: break;
  }
}

// ─── Setup ───────────────────────────────────────────────────────────────────
void setup() {
  Serial.begin(115200);
  delay(200);

  // Initialise LED first and force orange before any event can fire
  led.begin();
  setColor(255, 80, 0);
  logLine("=== Booting device ===");
  logLine("LED: orange (not provisioned)");

  shtcSensor.begin();
  pinMode(RESET_PIN, INPUT_PULLUP);

  if (digitalRead(RESET_PIN) == LOW) {
    logLine("RESET_PIN held — clearing all stored data and restarting...");
    WiFi.disconnect(true, true);
    prefs.begin("prov", false); prefs.clear(); prefs.end();
    prefs.begin("mqtt", false); prefs.clear(); prefs.end();
    delay(1000);
    ESP.restart();
  }

  WiFi.onEvent(onEvent);

  bool provisioned = false;
  network_prov_mgr_is_wifi_provisioned(&provisioned);
  logLine(String("Previously provisioned: ") + (provisioned ? "YES" : "NO"));

  if (provisioned) {
    logLine("Connecting to saved WiFi...");
    WiFi.begin();
  } else {
    logLine("Starting provisioning SoftAP: " PROV_SSID);
    // ledState stays LED_NOT_PROVISIONED (orange) until ARDUINO_EVENT_PROV_START
    // fires, then switches to LED_PROVISIONING (blink purple)
    WiFi.mode(WIFI_AP_STA);
    delay(100);

    network_prov_mgr_config_t config = {
      .scheme               = network_prov_scheme_softap,
      .scheme_event_handler = NETWORK_PROV_EVENT_HANDLER_NONE
    };
    network_prov_mgr_init(config);
    network_prov_mgr_endpoint_create("prov-data");
    network_prov_mgr_start_provisioning(NETWORK_PROV_SECURITY_0, NULL, PROV_SSID, NULL);
    network_prov_mgr_endpoint_register("prov-data", provDataHandler, NULL);
  }

  logLine("Setup complete.");
}

// ─── Loop ────────────────────────────────────────────────────────────────────
void loop() {
  updateLed();  // must run every iteration — drives all LED blinking

  if (!wifiReady) {
    delay(100);
    return;
  }

  if (!mqtt.connected()) connectMqtt();
  mqtt.loop();

  if (!systemStarted) {
    checkReprovisionButton();
    static unsigned long lastWait = 0;
    if (millis() - lastWait > 5000) {
      lastWait = millis();
      logLine("Waiting for 'start' command via MQTT...");
    }
    delay(100);
    return;
  }

  // ── Sensor read & publish ──────────────────────────────────────────────────
  shtcSensor.sample();

  float temp    = shtcSensor.readTempC();
  int   raw     = analogRead(sensorPin);
  int   soilPct = constrain(map(raw, DRY_VALUE, WET_VALUE, 0, 100), 0, 100);

  logLine("Temp: " + String(temp, 2) + " °C");
  logLine("Soil raw: " + String(raw));
  logLine("Soil moisture: " + String(soilPct) + " %");

  if (!tempSensorId.isEmpty()) publishJson(topic(tempSensorId, "temperature"), temperaturePayload(temp));
  if (!soilSensorId.isEmpty()) publishJson(topic(soilSensorId, "soil"),        soilPayload(soilPct));

  logLine("Cycle complete\n");

  // Non-blocking 5 s wait so button detection stays responsive during idle
  unsigned long waitUntil = millis() + 5000;
  while (millis() < waitUntil) {
    checkReprovisionButton();
    updateLed();
    mqtt.loop();
    delay(100);
  }
}

