//x9ptbkxb5bxx2kxx
#include <WiFi.h>                                  
  #include <PubSubClient.h>                                                               
  #include "SHTC3-SOLDERED.h"                                                                                                                                                                                                                                                                                       
  #include <network_provisioning/manager.h>                                                                                                                                                                                                                                                                         
  #include <network_provisioning/scheme_softap.h>                                                                                                                                                                                                                                                                   
  #include <Preferences.h>                                                                                                                                                                                                                                                                                          
  #include <HTTPClient.h>                                   
  #include <ArduinoJson.h>        // Library Manager: ArduinoJson by Benoit Blanchon

  // ─── Device identity ──────────────────────────────────────────────────────────
  // Must match deviceSsid returned by GET /api/provision for this device
  #define PROV_SSID  "SensorUnit_9"
  #define RESET_PIN  0

  // ─── Soil sensor calibration ──────────────────────────────────────────────────
  const int sensorPin = 10;
  const int DRY_VALUE = 2800;
  const int WET_VALUE = 0;

  // ─── Runtime state ────────────────────────────────────────────────────────────
  bool   wifiReady = false;
  String mqttHost  = "";
  int    mqttPort  = 1883;
  String tenantId  = "";
  String deviceId  = "";

  // ─── Peripherals ──────────────────────────────────────────────────────────────
  WiFiClient   wifiClient;
  PubSubClient mqtt(wifiClient);
  Preferences  prefs;
  SHTC3        shtcSensor;

  // ─── Helpers ──────────────────────────────────────────────────────────────────
  void logLine(const String& msg) { Serial.println("[LOG] " + msg); }

  String topic(const char* sensor) {
      return String("tenant_") + tenantId +
             "/device_"        + deviceId +
             "/sensor/"        + sensor;
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
      return String("{") +
          "\"value\":"     + String(temp, 2) + "," +
          "\"unit\":\"°C\"," +
          "\"timestamp\":\"" + isoTimestamp() + "\"" +
      "}";
  }

  String soilPayload(int soilPct) {
      return String("{") +
          "\"value\":"   + String(soilPct) + "," +
          "\"unit\":\"%\"," +
          "\"timestamp\":\"" + isoTimestamp() + "\"" +
      "}";
  }

  // ─── MQTT ─────────────────────────────────────────────────────────────────────
  void connectMqtt() {
      if (mqttHost.isEmpty()) return;
      mqtt.setServer(mqttHost.c_str(), mqttPort);
      mqtt.setKeepAlive(60);
      mqtt.setSocketTimeout(5);
      logLine("Connecting to MQTT broker...");
      while (!mqtt.connected()) {
          if (mqtt.connect("arduino-client", nullptr, nullptr, nullptr, 0, false, nullptr, true)) {
              logLine("MQTT connected to " + mqttHost);
          } else {
              logLine("MQTT failed. state=" + String(mqtt.state()));
              delay(1000);
          }
      }
  }

  // ─── Complete provisioning: POST token to backend ─────────────────────────────
  void completeProvisioning() {
      prefs.begin("prov", true);
      String token = prefs.getString("token", "");
      String host  = prefs.getString("host",  "");
      int    port  = prefs.getInt   ("port",  0);
      prefs.end();

      if (token.isEmpty() || host.isEmpty()) {
          logLine("No provisioning token — loading saved config.");
          prefs.begin("mqtt", true);
          mqttHost = prefs.getString("host",   "");
          mqttPort = prefs.getInt   ("port",   1883);
          tenantId = prefs.getString("tenant", "");
          deviceId = prefs.getString("device", "");
          prefs.end();
          return;
      }

      String url = "http://" + host + ":" + String(port) + "/api/provision/" + token;
      logLine("Completing provisioning: POST " + url);

      HTTPClient http;
      http.begin(url);
      http.addHeader("Content-Type", "application/json");
      int code = http.POST("{}");
      logLine("Provisioning response: " + String(code));

      if (code == 200) {
          String body = http.getString();
          logLine("Body: " + body);

          JsonDocument doc;
          if (deserializeJson(doc, body) == DeserializationError::Ok) {
              mqttHost = doc["mqttHost"].as<String>();
              mqttPort = doc["mqttPort"].as<int>();
              tenantId = String(doc["tenantId"].as<long>());
              deviceId = String(doc["deviceId"].as<long>());

              prefs.begin("mqtt", false);
              prefs.putString("host",   mqttHost);
              prefs.putInt   ("port",   mqttPort);
              prefs.putString("tenant", tenantId);
              prefs.putString("device", deviceId);
              prefs.end();

              // Clear one-time token
              prefs.begin("prov", false);
              prefs.remove("token");
              prefs.end();

              logLine("Done. Tenant=" + tenantId + " Device=" + deviceId +
                      " MQTT=" + mqttHost + ":" + String(mqttPort));
          }
      }
      http.end();
  }

  // ─── Custom endpoint: receives {token, host, port} from Android app ───────────
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

      // Must be heap-allocated — httpd calls free() on this pointer
      const char* resp = "{\"status\":\"ok\"}";
      size_t len = strlen(resp);
      *outbuf = (uint8_t*) malloc(len);
      if (*outbuf == NULL) {
          *outlen = 0;
          return ESP_ERR_NO_MEM;
      }
      memcpy(*outbuf, resp, len);
      *outlen = len;
      return ESP_OK;
  }

  // ─── WiFi / provisioning events ───────────────────────────────────────────────
  void onEvent(arduino_event_t *event) {
      switch (event->event_id) {
          case ARDUINO_EVENT_PROV_START:
              Serial.printf("[PROV] AP started. Connect phone to: %s\n", PROV_SSID);
              break;
          case ARDUINO_EVENT_PROV_CRED_RECV:
              Serial.println("[PROV] WiFi credentials received.");
              break;
          case ARDUINO_EVENT_PROV_CRED_SUCCESS:
              Serial.println("[PROV] Credentials accepted.");
              break;
          case ARDUINO_EVENT_PROV_CRED_FAIL:
              Serial.println("[PROV] Provisioning failed — bad credentials.");
              WiFi.disconnect(true, true);
              break;
          case ARDUINO_EVENT_PROV_END:
              Serial.println("[PROV] Provisioning session ended.");
              break;
          case ARDUINO_EVENT_WIFI_STA_GOT_IP:
              Serial.printf("[WiFi] Connected. IP: %s\n",
                  IPAddress(event->event_info.got_ip.ip_info.ip.addr).toString().c_str());
              wifiReady = true;
              completeProvisioning();
              break;
          case ARDUINO_EVENT_WIFI_STA_DISCONNECTED:
              Serial.println("[WiFi] Disconnected — reconnecting…");
              wifiReady = false;
              WiFi.begin();
              break;
          default:
              break;
      }
  }

  // ─── Setup ────────────────────────────────────────────────────────────────────
  void setup() {
      Serial.begin(115200);
      delay(1000);
      logLine("Booting device...");

      shtcSensor.begin();

      pinMode(RESET_PIN, INPUT_PULLUP);
      if (digitalRead(RESET_PIN) == LOW) {
          logLine("Resetting all stored data...");
          WiFi.disconnect(true, true);
          prefs.begin("prov", false); prefs.clear(); prefs.end();
          prefs.begin("mqtt", false); prefs.clear(); prefs.end();
          delay(1000);
      }

      WiFi.onEvent(onEvent);

      bool provisioned = false;
      network_prov_mgr_is_wifi_provisioned(&provisioned);

      if (provisioned) {
          logLine("Already provisioned — connecting to saved WiFi...");
          WiFi.begin();
      } else {                                                                                                                                                                                                                                                                                                      
          logLine("Starting provisioning AP...");           
          WiFi.mode(WIFI_AP_STA);   // ← add this line
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
  }

  // ─── Loop ─────────────────────────────────────────────────────────────────────
  void loop() {
      if (!wifiReady) {
          delay(500);
          return;
      }

      if (!mqtt.connected())
          connectMqtt();

      mqtt.loop();

      shtcSensor.sample();

      float temp = shtcSensor.readTempC();
      int raw     = analogRead(sensorPin);
      int soilPct = map(raw, DRY_VALUE, WET_VALUE, 0, 100);
      soilPct     = constrain(soilPct, 0, 100);

      logLine("Temp: " + String(temp, 2) + " °C");
      logLine("Soil raw: " + String(raw));
      logLine("Soil moisture: " + String(soilPct) + " %");

      publishJson(topic("temperature"), temperaturePayload(temp));
      publishJson(topic("soil"),        soilPayload(soilPct));

      logLine("Cycle complete\n");
      delay(5000);
  }
