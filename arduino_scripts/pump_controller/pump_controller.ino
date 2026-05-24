#include <WiFi.h>
#include <PubSubClient.h>
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
  LED_READY,            // solid blue    — MQTT connected, pump off
  LED_WORKING,          // blink green   — pump running
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
#define PROV_SSID  "PumpUnit_01"
#define BUTTON_PIN 0
#define RELAY_PIN  14

// ─── Runtime state ────────────────────────────────────────────────────────────
bool wifiReady      = false;
bool systemStarted  = false;
bool mqttConfirmSent = false;
bool pumpOn         = false;

String mqttHost = "";
int    mqttPort  = 1883;
String srvHost   = "";
int    srvPort   = 80;
String tenantId  = "";
String deviceId  = "";

// ─── Peripherals ──────────────────────────────────────────────────────────────
WiFiClient   wifiClient;
PubSubClient mqtt(wifiClient);
Preferences  prefs;

// ─── Helpers ──────────────────────────────────────────────────────────────────
void logLine(const String& msg) { Serial.println("[LOG] " + msg); }

String commandTopic() {
  return String("tenant_") + tenantId + "/device_" + deviceId + "/command";
}

String statusTopic() {
  return String("tenant_") + tenantId + "/device_" + deviceId + "/status";
}

void publishJson(const String& t, const String& payload) {
  bool ok = mqtt.publish(t.c_str(), payload.c_str(), true);
  logLine("Publish → " + t + " : " + payload + (ok ? " [OK]" : " [FAIL]"));
}

// ─── Relay control ────────────────────────────────────────────────────────────
void setPump(bool on) {
  pumpOn = on;
  digitalWrite(RELAY_PIN, on ? HIGH : LOW);
  ledState = on ? LED_WORKING : LED_READY;
  logLine(on ? "Pump ON" : "Pump OFF");
}

// ─── MQTT message callback ────────────────────────────────────────────────────
void onMqttMessage(char* topic, byte* payload, unsigned int length) {
  String msg((char*)payload, length);
  logLine("MQTT message on " + String(topic) + ": " + msg);

  JsonDocument doc;
  if (deserializeJson(doc, msg) != DeserializationError::Ok) return;

  String cmd = doc["command"].as<String>();
  if (cmd == "start") {
    logLine("Received 'start' — system active.");
    systemStarted = true;
    ledState = LED_READY;
    publishJson(statusTopic(), "{\"event\":\"started\"}");
  } else if (cmd == "stop") {
    logLine("Received 'stop' — system halted.");
    systemStarted = false;
    setPump(false);
    ledState = LED_STOPPED;
    publishJson(statusTopic(), "{\"event\":\"stopped\"}");
  }
}

// ─── Confirm MQTT connection to backend ──────────────────────────────────────
void confirmMqttConnection() {
  if (srvHost.isEmpty()) return;
  String url  = "http://" + srvHost + ":" + String(srvPort) + "/api/provision/confirm";
  String body = "{\"tenantId\":" + tenantId + ",\"deviceId\":" + deviceId + "}";
  logLine("Confirming MQTT: POST " + url);

  HTTPClient http;
  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  int code = http.POST(body);
  logLine("Confirm response: " + String(code));
  http.end();
}

// ─── Full reprovision ─────────────────────────────────────────────────────────
void requestReprovision() {
  if (srvHost.isEmpty() || tenantId.isEmpty() || deviceId.isEmpty()) {
    logLine("Cannot reprovision — srvHost/tenantId/deviceId not set");
    return;
  }

  setPump(false);
  ledState = LED_NOT_PROVISIONED;
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

  WiFi.disconnect(true, true);
  prefs.begin("prov", false); prefs.clear(); prefs.end();
  prefs.begin("mqtt", false); prefs.clear(); prefs.end();
  logLine("All data wiped — restarting into provisioning mode...");
  delay(500);
  ESP.restart();
}

// ─── Button: short press = toggle pump, 5 s hold = reprovision ───────────────
void checkButton() {
  static unsigned long pressedAt = 0;
  static bool wasPressed = false;

  bool pressed = (digitalRead(BUTTON_PIN) == LOW);

  if (pressed && !wasPressed) {
    pressedAt = millis();
    wasPressed = true;
    logLine("Button pressed...");
  } else if (!pressed && wasPressed) {
    unsigned long held = millis() - pressedAt;
    wasPressed = false;

    if (held >= 5000) {
      logLine("5 s hold — reprovisioning...");
      requestReprovision();
    } else if (held >= 50 && systemStarted) {
      logLine("Short press — toggling pump");
      setPump(!pumpOn);
      publishJson(statusTopic(), pumpOn ? "{\"event\":\"pump_on\"}" : "{\"event\":\"pump_off\"}");
    }
  }
}

// ─── MQTT connect + subscribe ─────────────────────────────────────────────────
unsigned long mqttRetryAt = 0;

void connectMqtt() {
  if (mqttHost.isEmpty()) return;
  if (millis() < mqttRetryAt) return;

  mqtt.setServer(mqttHost.c_str(), mqttPort);
  mqtt.setCallback(onMqttMessage);
  mqtt.setKeepAlive(60);
  mqtt.setSocketTimeout(5);

  logLine("Connecting to MQTT at " + mqttHost + ":" + String(mqttPort) + "...");

  if (mqtt.connect("pump-controller", nullptr, nullptr, nullptr, 0, false, nullptr, true)) {
    logLine("MQTT connected");
    mqtt.subscribe(commandTopic().c_str());
    logLine("Subscribed to: " + commandTopic());

    if (!mqttConfirmSent) {
      confirmMqttConnection();
      mqttConfirmSent = true;
    }

    ledState = LED_READY;
  } else {
    logLine("MQTT failed, state=" + String(mqtt.state()) + " — retrying in 5 s");
    mqttRetryAt = millis() + 5000;
  }
}

// ─── Complete provisioning: POST token to backend ────────────────────────────
void completeProvisioning() {
  ledState = LED_BACKEND_CONFIG;

  prefs.begin("prov", true);
  String token = prefs.getString("token", "");
  String host  = prefs.getString("host",  "");
  int    port  = prefs.getInt   ("port",  0);
  prefs.end();

  if (token.isEmpty() || host.isEmpty()) {
    logLine("No token — loading saved config.");

    prefs.begin("mqtt", true);
    mqttHost = prefs.getString("host",    "");
    mqttPort = prefs.getInt   ("port",    1883);
    srvHost  = prefs.getString("srvHost", "");
    srvPort  = prefs.getInt   ("srvPort", 80);
    tenantId = prefs.getString("tenant",  "");
    deviceId = prefs.getString("device",  "");
    prefs.end();

    logLine("Loaded — MQTT=" + mqttHost + ":" + String(mqttPort)
            + " tenant=" + tenantId + " device=" + deviceId);

    mqttConfirmSent = true;
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

      prefs.begin("mqtt", false);
      prefs.putString("host",    mqttHost);
      prefs.putInt   ("port",    mqttPort);
      prefs.putString("srvHost", srvHost);
      prefs.putInt   ("srvPort", srvPort);
      prefs.putString("tenant",  tenantId);
      prefs.putString("device",  deviceId);
      prefs.end();

      prefs.begin("prov", false);
      prefs.remove("token");
      prefs.end();

      logLine("Done. Tenant=" + tenantId + " Device=" + deviceId
              + " MQTT=" + mqttHost + ":" + String(mqttPort));
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
      ledState = LED_PROVISIONING;
      break;

    case ARDUINO_EVENT_PROV_CRED_RECV:
      logLine("Provisioning: WiFi credentials received.");
      break;

    case ARDUINO_EVENT_PROV_CRED_SUCCESS:
      logLine("Provisioning: credentials accepted.");
      break;

    case ARDUINO_EVENT_PROV_CRED_FAIL:
      logLine("Provisioning: FAILED — bad credentials.");
      ledState = LED_NOT_PROVISIONED;
      WiFi.disconnect(true, true);
      break;

    case ARDUINO_EVENT_PROV_END:
      logLine("Provisioning session ended.");
      break;

    case ARDUINO_EVENT_WIFI_STA_GOT_IP:
      logLine("WiFi connected. IP: "
              + IPAddress(event->event_info.got_ip.ip_info.ip.addr).toString());
      wifiReady = true;
      ledState = LED_BACKEND_CONFIG;
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

  led.begin();
  setColor(255, 80, 0);
  logLine("=== Booting pump controller ===");

  pinMode(BUTTON_PIN, INPUT_PULLUP);
  pinMode(RELAY_PIN, OUTPUT);
  digitalWrite(RELAY_PIN, LOW);  // relay off on boot

  if (digitalRead(BUTTON_PIN) == LOW) {
    logLine("Button held at boot — clearing all stored data and restarting...");
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
  updateLed();

  if (!wifiReady) {
    delay(100);
    return;
  }

  if (!mqtt.connected()) connectMqtt();
  mqtt.loop();

  checkButton();
  delay(20);
}
