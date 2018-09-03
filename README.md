# Unity-GB-Emu

Unity Gameboy Emulator written in C#.

## Current version: 0.1

### Features
* First working version
* Debugger and vram viewer tools included
* Compatible with following games:
  * Tetris
  * Super Mario Land
  * The Legend of Zelda: Link's awakening
  * Mega man
* Compatible with [Zal0's ZGB](https://github.com/Zal0/ZGB) or [GBDK](http://gbdk.sourceforge.net/) compiled games:
  * [Velcro golf with sheeps](https://github.com/brovador/velcro-golf-with-sheeps)
  * [GBSnake](https://github.com/brovador/GBsnake)

## Next version: 0.2

### Features
* Implement sound chip

## Known issues & next steps
* Pass all Blargg's tests
* Improve framerate
* Support for more cartdrige types
* Gameboy color support
* Improve debug tools
* Fix found errors in games:
  * Super Mario Land: pause window glitches
  * Dr. Mario: screen overflow
  * Alleway: input problems

## Blargg's tests status:
* Roms used: [Link](https://github.com/retrio/gb-test-roms)
* Tests passed:
	- [x] cpu_instrs
* Pending tests:
	- [ ] dmg_sound
	- [ ] instr_timing
	- [ ] interrupt_time
	- [ ] mem_timing-2
	- [ ] mem_timing
	- [ ] oam_bug

## Documentation and references used:

* Emulators:
  * [unity-gb](https://github.com/KonsomeJona/unity-gb)
  * [jsgb](https://github.com/Two9A/jsGB)
  * [Rubenknex/gameboy](https://github.com/Rubenknex/gameboy)
* Articles:
  * [Why did I spend 1.5 months creating a Gameboy emulator?](https://blog.rekawek.eu/2017/02/09/coffee-gb/)
  * [Writing a Gameboy emulator, Cinoop](https://cturt.github.io/cinoop.html)
  * [Imran Nazar: Gameboy emulation in javascript](http://imrannazar.com/GameBoy-Emulation-in-JavaScript:-The-CPU)
  * [A look at the gameboy bootstrap: let the fun begin!](https://realboyemulator.wordpress.com/2013/01/03/a-look-at-the-game-boy-bootstrap-let-the-fun-begin/)
* Documentation:
  * [Gameboy CPU opcodes](http://pastraiser.com/cpu/gameboy/gameboy_opcodes.html)
  * [Gameboy CPU manual 1.01](http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf)

## License
MIT
