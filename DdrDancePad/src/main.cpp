/* Touch Pad Read Example
   This example code is in the Public Domain (or CC0 licensed, at your option.)
   Unless required by applicable law or agreed to in writing, this
   software is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
   CONDITIONS OF ANY KIND, either express or implied.
*/
#include <Arduino.h>
#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/touch_pad.h"
#include "esp_log.h"

#define TOUCH_PAD_NO_CHANGE (-1)
#define TOUCH_THRESH_NO_USE (0)
#define TOUCH_FILTER_MODE_EN (0)
#define TOUCHPAD_FILTER_TOUCH_PERIOD (10)
typedef unsigned long ulong;
constexpr int _touchDebounce = 250;

static ulong _lastTouchTimes[10];

static void tp_example_touch_pad_init(void)
{
  for (int i = 0; i < TOUCH_PAD_MAX; i++)
  {
    touch_pad_config((touch_pad_t)i, TOUCH_THRESH_NO_USE);
  }
}

static void tp_example_touch_pad_thresh(void)
{
  uint16_t touch_value;
  for (int i = 0; i < TOUCH_PAD_MAX; i++)
  {
    touch_pad_read((touch_pad_t)i, &touch_value);
    touch_pad_set_thresh((touch_pad_t)i, touch_value * 0.6);
    touch_pad_set_trigger_mode(TOUCH_TRIGGER_BELOW);
  }
}

void setup()
{
  Serial.begin(921600);
  // Initialize touch pad peripheral.
  // The default fsm mode is software trigger mode.
  ESP_ERROR_CHECK(touch_pad_init());
  // Set reference voltage for charging/discharging
  // In this case, the high reference valtage will be 2.7V - 1V = 1.7V
  // The low reference voltage will be 0.5
  // The larger the range, the larger the pulse count value.
  touch_pad_set_voltage(TOUCH_HVOLT_2V7, TOUCH_LVOLT_0V5, TOUCH_HVOLT_ATTEN_1V);
  touch_pad_set_trigger_mode(TOUCH_TRIGGER_BELOW);
  tp_example_touch_pad_init();

  Serial.println("Touch Sensor normal mode read, the output format is: \nTouchpad num:[raw data]\n\n");

  tp_example_touch_pad_thresh();
}

void loop()
{
  uint16_t touch_value;
  uint16_t touch_thresh;

  ulong now = millis();
  for (int i = 0; i < TOUCH_PAD_MAX; i++)
  {
    touch_pad_read((touch_pad_t)i, &touch_value);
    touch_pad_get_thresh((touch_pad_t)i, &touch_thresh);

    if (touch_value < touch_thresh && (now - _lastTouchTimes[i]) > _touchDebounce)
    {
      Serial.printf("T%d\n", i);
      _lastTouchTimes[i] = now;
    }
  }

  vTaskDelay(10);
}