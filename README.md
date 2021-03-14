# DDR Twist 'N Shout

This new spin on the classic DDR experience is perfect for the headache enthusiast!  The dance pads you know and love have been augmented with a new spin movement and "streak incentivizer".

![Gamepad](https://raw.githubusercontent.com/TIPConsulting/DDR-TwistNShout/master/Documents/Gamepad.jpg)

## Hardware
- 2x ESP32 microcontrollers
- Wearable vibration motor
- An assembled [HairTrigger glove](https://github.com/TIPConsulting/ESP32_HairTrigger)
- Misc wires
- Aluminum foil
- Painters tape
- Conductive tape
- Conductive thread

## Inspiration

My goal was to integrate some form of wearable electronics into DDR.  At first, I came up with the idea to zap players when they missed notes, but I decided that might not be the safest idea 🙄.  So instead, I took inspiration from surprise hand buzzers - they use vibrations to simulate an electric shock.  For best results, I decided to put the vibrator in a headband right on the player's forehead.

I knew I would need a wireless connection to let players move freely while they played, so I also decided to use to my advantage for input.  I already had a working wireless gyro from my recent HairTrigger project, so I decided to repurpose that into a DDR input.  I used the gyro to detect spinning movements to improve the wearable aspect and elevate the project from such a simple clone.

## Design

The project has 3 components, the UI, the gamepad, and the wearable.  These components (and the integrations linking them) represent several significant technical features

- Windows GUI
- Microcontroller code (both C# and C++)
- WiFi integration
- Serial integration
- Realtime bidirectional communication
- Motion sensing

### UI

Since this is a DDR project, I knew I would need a DDR visual.  I created a UI from scratch for this project.  It handles gameplay rules and integrates input the 2 auxiliary devices.  I wanted to keep the game pretty simple because of time constraints, but I also wanted it to be immediately recognizable as a DDR spinoff.  I decided that the best way was to keep the classic falling symbols and use arrow graphics from the game.

![GUI](https://raw.githubusercontent.com/TIPConsulting/DDR-TwistNShout/master/Documents/DdrGuiScreenshot.PNG)

### Gamepad

I considered several options for the gamepad.  My first consideration was IMU shoes to detect foot movement.  I decided that would be too complex and accuracy would suffer.  I also quickly dismissed buttons or switches on the floor - that would take too much precision. 

In the end, I went with large capacitive touchpads on the floor. 

The gamepad uses large sheets of foil connected to the touch pins of an ESP32.  The controller reads the inputs and relays them to a computer over serial port.

![Gamepad](https://raw.githubusercontent.com/TIPConsulting/DDR-TwistNShout/master/Documents/Gamepad.jpg)

Since spinning is such an important aspect of this game, I thought that socks might help make that easier.  Normal socks wouldn't work with the touch sensitive gamepads, so I took a pair and added conductive tape to make them captouch friendly.

![SuperSocks](https://raw.githubusercontent.com/TIPConsulting/DDR-TwistNShout/master/Documents/SuperSock.JPG)

### Wearable

The wearable has 2 jobs (1) gently remind the user to hit the proper notes, and (2) detect spinning motions.  To save some time on this project, I reused the glove from HairTrigger since its already configured with an ESP32 and gyro.  I then made a headband with a vibrator and LED, and joined them together with some long wires.

The controller detects sustained spinning motions to figure out if the user is moving.  If it detects a large enough spin, it sends a signal to the game host over WiFi.  Similarly, when the player misses notes in the game, the gamehost sends a command to the wearable to activate the vibrator.

![Headband](https://raw.githubusercontent.com/TIPConsulting/DDR-TwistNShout/master/Documents/Headband.JPG)

![HairTrigger](https://raw.githubusercontent.com/TIPConsulting/ESP32_HairTrigger/master/Diagrams/Complete1.JPG)
