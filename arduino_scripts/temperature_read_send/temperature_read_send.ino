#include <BMP180-SOLDERED.h>
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>

#define SOIL_DIGITAL 12

const char* ssid = "Speedport-031111";
const char* password = "x9ptbkxb5bxx2kxx";
const char* serverUrl = "http://192.168.1.123:5017/api/Senzors/temperature";

Bmp_180 bmp180;

// ─── WiFi ───────────────────────────────────────────────
void connectWiFi() {
  WiFi.begin(ssid, password);
  Serial.print("Spajanje na WiFi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nSpojeno!");
}

// ─── BMP180 ─────────────────────────────────────────────
double readTemperature() {
  double temperature = 0;
  char ret = bmp180.startTemperature();
  if (ret != 0) {
    delay(ret);
    bmp180.getTemperature(temperature);
  } else {
    Serial.println("Greška čitanja temperature!");
  }
  return temperature;
}

float readPressure(double& temperature) {
  double pressure = 0;
  char ret = bmp180.startPressure(3);
  if (ret != 0) {
    delay(ret);
    bmp180.getPressure(pressure, temperature);
  } else {
    Serial.println("Greška čitanja tlaka!");
  }
  return (float)pressure;
}

// ─── Soil sensor ────────────────────────────────────────
int readSoilMoisture() {
    int analogVal = analogRead(A0);
    
    Serial.println("Raw ADC: " + String(analogVal));

int moisture = constrain(map(analogVal, 710, 600, 0, 100), 0, 100);
return moisture;
}

bool readSoilDigital() {
  return digitalRead(SOIL_DIGITAL);
}

// ─── Print ──────────────────────────────────────────────
void printReadings(float temp, float pres, int moisture, bool soilDigital) {
  Serial.println("-----------------------------");
  Serial.println("Temperatura:      " + String(temp) + " °C");
  Serial.println("Tlak:             " + String(pres) + " mBar");
  Serial.println("Vlaga (analogno): " + String(moisture) + "%");
  Serial.println("Vlaga (digital):  " + String(soilDigital));
  Serial.println("-----------------------------");
}

// ─── Setup ──────────────────────────────────────────────
void setup() {
  Serial.begin(9600);
  connectWiFi();

  if (bmp180.begin())
    Serial.println("BMP180 inicijaliziran!");
  else
    Serial.println("BMP180 greška!");

  pinMode(SOIL_DIGITAL, INPUT);
}

// ─── Loop ───────────────────────────────────────────────
void loop() {
  double temperature = readTemperature();
  float pressure     = readPressure(temperature);
  int moisture       = readSoilMoisture();
  bool soilDigital   = readSoilDigital();

  printReadings(temperature, pressure, moisture, soilDigital);
  

  delay(5000);
}