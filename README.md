# WinNTC1750
A NordicTrack Commercial 1750 Treadmill Controller for Windows (not Android)

I was gifted a broken NordicTrack Commercial 1750 treadmill (NTL17124.7).
After checking all boards on the machine I realized the tablet (MP14-ARGON2-Back-E) that came with it was bricked.
I have searched for a replacement unit but it was too expensive.
I plugged the tablet to my computer and realized it was on detected as a VID 0E8D (Mediatek) and VID 0003 (Mediatek USB Port BROM) and it would disconnect and sometimes with VID 2000 (Mediatek Preloader USB VCOM).
I knew I could use a flash tool to reflash it if I put my hands on the firmware (and the scatter file), but I i could not find them.
The tablet was connected, inside the console, to a brainboard called "Good Wolf Universal ZH0058" via a standart 5 wire connector, but the board itself had a USB mark on it, so I gave it a shot and connected to my PC.

The board was detected as VID 213C and PID 0002 (ICON Generic HID).

I found 2 other repositories that had information about the protocol used, but their hardware was different and they were just aditional apps to install on the original android tablet.
https://github.com/totokuku/opentread
https://github.com/keranm/strideApp

Since I had no android tablet arround, but had a few windows notebooks, I decided to code something to talk to the board.
I used Claude to help me understand the protocol and test it.

The moment the board is connected, and the handshake finished, all the hardware buttons start working, except the START button.

