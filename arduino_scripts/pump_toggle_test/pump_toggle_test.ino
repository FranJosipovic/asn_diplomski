#include <Adafruit_NeoPixel.h>

#define LED_PIN    2
#define LED_COUNT  1
#define BUTTON_PIN 0
#define RELAY_PIN  14

Adafruit_NeoPixel led(LED_COUNT, LED_PIN, NEO_GRB + NEO_KHZ800);

bool pumpOn = false;
unsigned long ledTimer = 0;
bool ledOn = false;

void setPump(bool on) {
  pumpOn = on;
  digitalWrite(RELAY_PIN, on ? HIGH : LOW);
}

void updateLed() {
  if (pumpOn) {
    // blink green while pump is running
    if (millis() - ledTimer > 500) {
      ledTimer = millis();
      ledOn = !ledOn;
      led.setPixelColor(0, led.Color(0, ledOn ? 255 : 0, 0));
      led.show();
    }
  } else {
    // solid red when pump is off
    led.setPixelColor(0, led.Color(255, 0, 0));
    led.show();
  }
}

void checkButton() {
  static unsigned long pressedAt = 0;
  static bool wasPressed = false;

  bool pressed = (digitalRead(BUTTON_PIN) == LOW);

  if (pressed && !wasPressed) {
    pressedAt = millis();
    wasPressed = true;
  } else if (!pressed && wasPressed) {
    wasPressed = false;
    if (millis() - pressedAt >= 50) {  // debounce
      setPump(!pumpOn);
    }
  }
}

void setup() {
  Serial.begin(9600);
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  pinMode(RELAY_PIN, OUTPUT);
  digitalWrite(RELAY_PIN, LOW);

  led.begin();
  led.setPixelColor(0, led.Color(255, 0, 0));
  led.show();

  Serial.println("Ready — press button to toggle pump");
}

void loop() {
  checkButton();
  updateLed();
  delay(20);
}
