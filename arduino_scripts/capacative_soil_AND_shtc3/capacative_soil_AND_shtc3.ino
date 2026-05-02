#include "SHTC3-SOLDERED.h"

SHTC3 shtcSensor;
const int sensorPin = 10;

// *** UPIŠI SVOJE IZMJERENE VRIJEDNOSTI OVDJE ***
const int DRY_VALUE = 2800;   // Vrijednost u suhom zraku
const int WET_VALUE  = 0;  // Vrijednost u vodi

void setup() {
    shtcSensor.begin();
    Serial.begin(115200);
    
    Serial.println("=== KALIBRACIJA SENZORA ===");
    Serial.print("Suho (0%):  "); Serial.println(DRY_VALUE);
    Serial.print("Mokro (100%): "); Serial.println(WET_VALUE);
    Serial.println("===========================");
}

void loop() {
    shtcSensor.sample();

    int sensorValue = analogRead(sensorPin);
    int percentage  = map(sensorValue, DRY_VALUE, WET_VALUE, 0, 100);
    percentage      = constrain(percentage, 0, 100);

    Serial.print("Temp: ");
    Serial.print(shtcSensor.readTempC(), 2);
    Serial.println(" °C");

    Serial.print("Hum: ");
    Serial.print(shtcSensor.readHumidity(), 2);
    Serial.println(" %");

    Serial.print("Soil Raw Value: ");
    Serial.println(sensorValue);

    Serial.print("Soil Moisture: ");
    Serial.print(percentage);
    Serial.println(" %");

    Serial.println("---");
    delay(5000);
}