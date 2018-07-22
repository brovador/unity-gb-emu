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

		#warning this should be in memory
		GPUMode gpuMode;

		byte CurrentLine {
			get { return mmu.Read(0xFF44); }
			set { mmu.Write(0xFF44, (byte)value); }
		}

		uint clock;

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
			CurrentLine = 0;
			clock = 0;
		}


		public void Step()
		{
			clock += cpu.timers.lastOpCycles;

			switch (gpuMode) {

			//HBlank
			case GPUMode.HBlank:
				if (clock >= HORIZONAL_BLANK_CYCLES) {
					clock -= (uint)HORIZONAL_BLANK_CYCLES;
					CurrentLine++;

					if (CurrentLine == MAX_LINES) {
						gpuMode = GPUMode.VBlank;
						mmu.SetInterrupt(MMU.InterruptType.VBlank);
						DrawFrame();
					}
				}
				break;
			
			//VBlank
			case GPUMode.VBlank:
				uint t = (uint)(SCANLINE_OAM_CYCLES + SCANLINE_VRAM_CYCLES + HORIZONAL_BLANK_CYCLES);
				if (clock >= t) {
					clock -= t;
					CurrentLine++;

					if (CurrentLine > 153) {
						gpuMode = GPUMode.OAMRead;
						CurrentLine = 0;
					}
				}
				break;
			
			//OAM Read
			case GPUMode.OAMRead:
				if (clock >= SCANLINE_OAM_CYCLES) {
					clock -= (uint)SCANLINE_OAM_CYCLES;
					gpuMode = GPUMode.VRAMRead;
				}
				break;
			
			//VRAM Read
			case GPUMode.VRAMRead:
				if (clock >= SCANLINE_VRAM_CYCLES) {
					clock -= (uint)SCANLINE_VRAM_CYCLES;
					gpuMode = GPUMode.HBlank;
				}
				break;
			}
		}


		public void DrawFrame()
		{
//			Debug.Log(string.Format("Frame ellapsed time: {0}", (Time.realtimeSinceStartup - t)));
//			t = Time.realtimeSinceStartup;
		}
	}
}
