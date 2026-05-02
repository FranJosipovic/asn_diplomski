const int trigPin = 9;
const int echoPin = 10;

void setup() {
  Serial.begin(115200);
  pinMode(trigPin, OUTPUT);
  pinMode(echoPin, INPUT);
}

void loop() {
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(trigPin, LOW);

  long trajanje = pulseIn(echoPin, HIGH, 30000);

  if (trajanje == 0) {
    Serial.println("Nema signala / izvan dometa");
  } else {
    float udaljenost = trajanje * 0.0343 / 2.0;
    Serial.print("Udaljenost: ");
    Serial.print(udaljenost, 1);
    Serial.println(" cm");
  }

  delay(500);
}