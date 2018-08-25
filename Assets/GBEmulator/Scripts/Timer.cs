using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace brovador.GBEmulator {
	public class Timer {

		public enum Speed {
			clk_4096Hz = 1024,
			clk_262144Hz = 16,
			clk_65536Hz = 64,
			clk_16384Hz = 256,
		}

		public byte DIV {
			get { return mmu.Read(0xFF04); }
			set { mmu.Write(0xFF04, value, true); }
		}

		//Counter
		public byte TIMA {
			get { return mmu.Read(0xFF05); }
			set { mmu.Write(0xFF05, value); }
		}

		//Modulo
		public byte TMA {
			get { return mmu.Read(0xFF06); }
			set { mmu.Write(0xFF06, value); }
		}

		//Control
		public byte TAC {
			get { return mmu.Read(0xFF07); }
			set { mmu.Write(0xFF07, value); }
		}

		public Speed TimerSpeed {
			get { return (Speed)(TAC & 0x03); }
			set { TAC = (byte)((TAC & 0xFC) + value); }
		}

		public bool IsRunning {
			get { return (TAC & 0x04) != 0; }
			set { TAC = (byte)((TAC & 0xFB) + (value?0x04:0x00)); }
		}


		MMU mmu;
		//Timer clock:  	 262144Hz (1/16 cpu speed)
		uint clock = 0;
		uint clockTmp = 0;

		//Divider clock:	  16384Hz (1/16 timer clock speed)
		uint dividerClockTmp = 0;

		//Counter clock 00:	   4096Hz (1/64 timer clock speed)
		//Counter clock 01:	 262144Hz (1 timer clock speed)
		//Counter clock 10:	  65536Hz (1/4 timer clock speed)
		//Counter clock 11:	  16384Hz (1/16 timer clock speed)


		public Timer(MMU mmu)
		{
			this.mmu = mmu;
			clock = 0;
			clockTmp = 0;
			dividerClockTmp = 0;
		}


		public void Step(uint opCycles)
		{
			clockTmp += opCycles;
			dividerClockTmp += opCycles;

			//Main clock runs at: 4.194304MHz

			//Divider runs at: 16384Hz
			if (dividerClockTmp >= 256) {
				dividerClockTmp -= 256;
				DIV++;
			}

			if (IsRunning) {
				while (clockTmp >= (int)TimerSpeed) {
					clockTmp -= (uint)TimerSpeed;
					TIMA++;
					if (TIMA == 0) {
						mmu.SetInterrupt(MMU.InterruptType.TimerOverflow);
						TIMA = TMA;
					}
				}
			}

//			clockTmp += opCycles;
//			//1/16 cpu speed: increment main clock
//			if (clockTmp >= 16) {
//				clockTmp -= 16;
//				clock++;
//
//				//1/16 Increment divider
//				dividerClockTmp++;
//				if (dividerClockTmp == 16) {
//					dividerClockTmp = 0;
//					DIV++;
//				}
//			}
//
//			if (IsRunning) {
//				//1/x Increment counter
//				while (clock >= (int)TimerSpeed) {
//					clock -= (uint)TimerSpeed;
//					TIMA++;
//					if (TIMA == 0) {
//						mmu.SetInterrupt(MMU.InterruptType.TimerOverflow);
//						TIMA = TMA;
//					}
//				}
//			}
		}
	}
}
