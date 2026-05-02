#include "Simple-soil-sensor-easyC-SOLDERED.h"
#define ANALOG_PIN 8

SimpleSoilSensor sensor(ANALOG_PIN);

#define CALIBRATION_RESISTANCE_TIP_IN_WATER  15000
#define CALIBRATION_RESISTANCE_FULL_IN_WATER 7000


void setup(){
  Serial.begin(115200);

    // Initialize the sensor
    sensor.begin();

    // Sensor will work just fine if it is not calibrated
    // but since it is relying on resistance of water and
    // and water significantly changes resistance if it is
    // filled with particles, it smart idea to calibrate sometimes.
    // For calibration it is needed to measure resistance of sensor
    // when only tip is in water(about 5mm of sensor) and when
    // pads are completely in water and write them in
    // CALIBRATION_RESISTANCE_TIP_IN_WATER and CALIBRATION_RESISTANCE_FULL_IN_WATER
    sensor.calibrate(CALIBRATION_RESISTANCE_TIP_IN_WATER, CALIBRATION_RESISTANCE_FULL_IN_WATER);

    // If different microcontroller with different bit width
    // is used, it should be set using this function
    sensor.setADCWidth(12);
}

void loop(){
  Serial.print("Raw value of sensor: "); // Print information message
    Serial.print(sensor.getValue());       // Prints percent value of soil sensor
    Serial.print("% ");
    Serial.println(sensor.getRawValue()); // Prints raw value of soil sensor

    Serial.print("Resistance of sensor: "); // Print information message
    Serial.print(sensor.getResistance());   // Prints resistance of soil sensor
    Serial.println(" Ohms.");               // Print information message

    Serial.print("Humidity: ");         // Print information message
    Serial.print(sensor.getHumidity()); // Prints raw value of soil sensor
    Serial.println(" %.");              // Print information message

    // Wait a bit before next reading
    delay(500);
}