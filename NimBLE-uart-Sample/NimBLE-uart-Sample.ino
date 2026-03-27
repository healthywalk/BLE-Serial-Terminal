/*
  NimBLE-uart-Sample.ino
  Based on nkolban's example : https://github.com/nkolban/ESP32_BLE_Arduino/blob/master/examples/BLE_uart/BLE_uart.ino
  Based on H2zero's example : https://github.com/h2zero/NimBLE-Arduino/blob/master/examples/NimBLE_Server/NimBLE_Server.ino
  
*/
#include <NimBLEDevice.h>

NimBLEServer *pServer = NULL;
NimBLECharacteristic *pTxCharacteristic;
bool deviceConnected = false;
bool oldDeviceConnected = false;

#define SERVICE_UUID           "6E400001-B5A3-F393-E0A9-E50E24DCCA9E"  // UART service UUID
#define CHARACTERISTIC_UUID_RX "6E400002-B5A3-F393-E0A9-E50E24DCCA9E"
#define CHARACTERISTIC_UUID_TX "6E400003-B5A3-F393-E0A9-E50E24DCCA9E"

#define DEVICE_NAME "Uart-NBLE"

class MyServerCallbacks : public NimBLEServerCallbacks {
  void onConnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo) override {
    deviceConnected = true;
    delay(10);
    Serial.println("connected");
    delay(10);
  };

  void onDisconnect(NimBLEServer* pServer, NimBLEConnInfo& connInfo, int reason) override {
    deviceConnected = false;
    delay(10);
    Serial.println("disconnected");
    delay(10);
  }

  void onMTUChange(uint16_t MTU, NimBLEConnInfo& connInfo) override {
      Serial.printf("MTU updated: %u for connection ID: %u\n", MTU, connInfo.getConnHandle());
  }
};

class MyCallbacks : public NimBLECharacteristicCallbacks {
  void onWrite(NimBLECharacteristic* ch, NimBLEConnInfo& info) override {
    String rxValue = ch->getValue();

    if (rxValue.length() > 0) {
      rxValue.trim();
      Serial.printf(">> received string : %s\n", rxValue);
      delay(10);
      //Reply as is
      pTxCharacteristic->setValue(rxValue);
      pTxCharacteristic->notify();
      delay(10);
    }
  }

  void onStatus(NimBLECharacteristic* pCharacteristic, int code) override {
      Serial.printf("Notification/Indication return code: %d, %s\n", code, NimBLEUtils::returnCodeToString(code));
  }
};

void setup() {
  delay(300);
  Serial.begin(115200);
  delay(300);

  // Create the NimBLE Device
  NimBLEDevice::init(DEVICE_NAME);
  NimBLEDevice::setSecurityAuth(false, false, false);
  NimBLEDevice::setMTU(247);

  // Create the NimBLE Server
  pServer = NimBLEDevice::createServer();
  pServer->setCallbacks(new MyServerCallbacks());

  // Create the NimBLE Service
  NimBLEService *pService = pServer->createService(SERVICE_UUID);

  // Create a NimBLE Characteristic
  pTxCharacteristic = pService->createCharacteristic(CHARACTERISTIC_UUID_TX, 
        NIMBLE_PROPERTY::NOTIFY);

  NimBLECharacteristic *pRxCharacteristic = pService->createCharacteristic(CHARACTERISTIC_UUID_RX, 
        NIMBLE_PROPERTY::WRITE | NIMBLE_PROPERTY::WRITE_NR);

  pRxCharacteristic->setCallbacks(new MyCallbacks());

  // Start the service
  pService->start();

  // Start advertising
  NimBLEAdvertising *pAdvertising = NimBLEDevice::getAdvertising();
  NimBLEAdvertisementData advData = NimBLEAdvertisementData();
  advData.addServiceUUID(SERVICE_UUID);
  pAdvertising->setAdvertisementData(advData);
  NimBLEAdvertisementData scanData = NimBLEAdvertisementData();;
  scanData.setName(DEVICE_NAME);
  scanData.addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponseData(scanData);
  pAdvertising->enableScanResponse(true);
  //pAdvertising->setMinPreferred(0x06);  // helps with iPhone connections
  //pAdvertising->setMaxPreferred(0x12);
  pAdvertising->setMinInterval(0x20); // 40ms helps with WINPC connections
  pAdvertising->setMaxInterval(0x40); // 80ms
  pAdvertising->start();

  Serial.println();
  Serial.println(DEVICE_NAME);
  Serial.println("Waiting a client connection to notify...");
}

void loop() {
  if (deviceConnected) {
    static uint32_t last = 0;
    // Periodic transmission
    if (millis() - last > 5000) {
        last = millis();
        const char* msg = "Hello from Peripheral < " DEVICE_NAME " >\n";
        pTxCharacteristic->setValue(msg);
        pTxCharacteristic->notify();
    }
  }

  // disconnecting
  if (!deviceConnected && oldDeviceConnected) {
    delay(500);                   // give the bluetooth stack the chance to get things ready
    pServer->startAdvertising();  // restart advertising
    Serial.println(">> Device disconnected");
    Serial.println(DEVICE_NAME);
    Serial.println("Started advertising again...");
    oldDeviceConnected = false;
  }
  // connecting
  if (deviceConnected && !oldDeviceConnected) {
    delay(500);                   // give the bluetooth stack the chance to get things ready
    oldDeviceConnected = true;
    Serial.println(">> Device connected");
  }
}
