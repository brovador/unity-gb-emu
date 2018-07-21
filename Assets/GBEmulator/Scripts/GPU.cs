using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace brovador.GBEmulator {
	
	public class GPU {

		enum GPUMode {
			HBlank,
			VBlank,
			OAMRead,
			VRAMRead
		}

		CPU cpu;
		MMU mmu;

		GPUMode gpuMode;
		int line;

		uint clock;
		uint lastCPUclock;

		public int HORIZONAL_BLANK_CYCLES = 204;
		public int VERTICAL_BLANK_CYCLES = 4560;
		public int SCANLINE_OAM_CYCLES = 80;
		public int SCANLINE_VRAM_CYCLES = 172;

		public int MAX_LINES = 143;

		public GPU(CPU cpu, MMU mmu) 
		{
			this.cpu = cpu;
			this.mmu = mmu;

			gpuMode = GPUMode.HBlank;
			line = 0;
			clock = 0;
			lastCPUclock = cpu.timers.t;
		}


		public void Step()
		{
			clock += (cpu.timers.t - lastCPUclock);
			lastCPUclock = cpu.timers.t;

			switch (gpuMode) {

			//HBlank
			case GPUMode.HBlank:
				if (clock >= HORIZONAL_BLANK_CYCLES) {
					clock = 0;
					line++;

					if (line == MAX_LINES) {
						gpuMode = GPUMode.VBlank;
						//TODO: update graphics
					}
				}
				break;
			
			//VBlank
			case GPUMode.VBlank:
				if (clock >= (SCANLINE_OAM_CYCLES + SCANLINE_VRAM_CYCLES + HORIZONAL_BLANK_CYCLES)) {
					clock = 0;
					line++;

					if (line > 153) {
						gpuMode = GPUMode.OAMRead;
						line = 0;
						mmu.SetInterrupt(MMU.InterruptType.VBlank);
					}
				}
				break;
			
			//OAM Read
			case GPUMode.OAMRead:
				if (clock >= SCANLINE_OAM_CYCLES) {
					clock = 0;
					gpuMode = GPUMode.VRAMRead;
				}
				break;
			
			//VRAM Read
			case GPUMode.VRAMRead:
				if (clock >= SCANLINE_VRAM_CYCLES) {
					clock = 0;
					gpuMode = GPUMode.HBlank;
				}
				break;
			}
		}
	}
}
