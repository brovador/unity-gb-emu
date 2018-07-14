//Disable assignement to the same variable warning
#pragma warning disable 1717

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace brovador.GBEmulator {

	public class CPU {

		public MMU mmu;

		public struct Timers {
			public uint t;
			public uint m;
		}

		public struct Registers
		{
			public byte A, B, C, D, E, H, L, F;
			public UInt16 PC;
			public UInt16 SP;

			public UInt16 AF { get { return (UInt16)(((UInt16)A) << 8 + F); } }
			public UInt16 BC { get { return (UInt16)(((UInt16)B) << 8 + C); } }
			public UInt16 DE { get { return (UInt16)(((UInt16)D) << 8 + E); } }
			public UInt16 HL { get { return (UInt16)(((UInt16)H) << 8 + L); } }

			public void Dec_HL() {
				L-=1; if (L==255) H-=1;
			}

			public void Inc_HL() {
				L+=1; if (L==0) H+=1;
			}
		}

		public Timers timers;
		public Registers registers;

		public byte[] mTimes = {
			1, 3, 1, 1, 1, 1, 2, 1, 3, 1, 1, 1, 1, 1, 2, 1,
			2, 3, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 2, 1,
			2, 3, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 2, 1,
			2, 3, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 2, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 3, 3, 3, 1, 2, 1, 1, 1, 3, 1, 3, 3, 2, 1,
			1, 1, 3, 0, 3, 1, 2, 1, 1, 1, 3, 0, 3, 0, 2, 1,
			2, 1, 1, 0, 0, 1, 2, 1, 2, 1, 3, 0, 0, 0, 2, 1,
			2, 1, 1, 1, 0, 1, 2, 1, 2, 1, 3, 1, 0, 0, 2, 1,
		};
			
		public byte[] tTimes = {
			4,  12, 8,  8,  4,  4,  8,  4,  20, 8,  8,  8, 4,  4,  8, 4,
			4,  12, 8,  8,  4,  4,  8,  4,  12, 8,  8,  8, 4,  4,  8, 4,
			8,  12, 8,  8,  4,  4,  8,  4,  8,  8,  8,  8, 4,  4,  8, 4,
			8,  12, 8,  8,  12, 12, 12, 4,  8,  8,  8,  8, 4,  4,  8, 4,
			4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4, 4,  4,  8, 4,
			4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4, 4,  4,  8, 4,
			4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4, 4,  4,  8, 4,
			8,  8,  8,  8,  8,  8,  4,  8,  4,  4,  4,  4, 4,  4,  8, 4,
			4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4, 4,  4,  8, 4,
			4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4, 4,  4,  8, 4,
			4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4, 4,  4,  8, 4,
			4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4, 4,  4,  8, 4,
			8,  12, 12, 16, 12, 16, 8,  16, 8,  16, 12, 4, 12, 24, 8, 16,
			8,  12, 12, 0,  12, 16, 8,  16, 8,  16, 12, 0, 12, 0,  8, 16,
			12, 12, 8,  0,  0,  16, 8,  16, 16, 4,  16, 0, 0,  0,  8, 16,
			12, 12, 8,  4,  0,  16, 8,  16, 12, 8,  16, 4, 0,  0,  8, 16,	
		};
			
		public System.Action[] operations;

		public CPU(MMU mmu) {
			this.mmu = mmu;

			timers.t = 0;
			timers.m = 0;

			registers.A = 0;
			registers.B = 0;
			registers.C = 0;
			registers.D = 0;
			registers.E = 0;
			registers.H = 0;
			registers.L = 0;

			operations = new System.Action[256];

		}

		public void Step()
		{
			var op = mmu.Read(registers.PC++);
			operations[op]();
			timers.t += tTimes[op];
			timers.m += mTimes[op];
		}


		void NOP() {}


		//ld-nn-n
		void OP_06() { registers.B=mmu.Read(registers.PC++); } //LD B n
		void OP_0E() { registers.C=mmu.Read(registers.PC++); } //LD C n
		void OP_16() { registers.D=mmu.Read(registers.PC++); } //LD D n
		void OP_1E() { registers.E=mmu.Read(registers.PC++); } //LD E n
		void OP_26() { registers.H=mmu.Read(registers.PC++); } //LD H n
		void OP_2E() { registers.L=mmu.Read(registers.PC++); } //LD L

		//ld-r1-r2
		void OP_7F() { registers.A=registers.A; } //LD A A
		void OP_78() { registers.A=registers.B; } //LD A B
		void OP_79() { registers.A=registers.C; } //LD A C
		void OP_7A() { registers.A=registers.D; } //LD A D
		void OP_7B() { registers.A=registers.E; } //LD A E
		void OP_7C() { registers.A=registers.H; } //LD A H
		void OP_7D() { registers.A=registers.L; } //LD A L
		void OP_7E() { registers.A=mmu.Read(registers.HL); } //LD A (HL)
		void OP_40() { registers.B=registers.B; } //LD B B
		void OP_41() { registers.B=registers.C; } //LD B C
		void OP_42() { registers.B=registers.D; } //LD B D
		void OP_43() { registers.B=registers.E; } //LD B E
		void OP_44() { registers.B=registers.H; } //LD B H
		void OP_45() { registers.B=registers.L; } //LD B L
		void OP_46() { registers.B=mmu.Read(registers.HL); } //LD B (HL)
		void OP_48() { registers.C=registers.B; } //LD C B
		void OP_49() { registers.C=registers.C; } //LD C C
		void OP_4A() { registers.C=registers.D; } //LD C D
		void OP_4B() { registers.C=registers.E; } //LD C E
		void OP_4C() { registers.C=registers.H; } //LD C H
		void OP_4D() { registers.C=registers.L; } //LD C L
		void OP_4E() { registers.C=mmu.Read(registers.HL); } //LD C (HL)
		void OP_50() { registers.D=registers.B; } //LD D B
		void OP_51() { registers.D=registers.C; } //LD D C
		void OP_52() { registers.D=registers.D; } //LD D D
		void OP_53() { registers.D=registers.E; } //LD D E
		void OP_54() { registers.D=registers.H; } //LD D H
		void OP_55() { registers.D=registers.L; } //LD D L
		void OP_56() { registers.D=mmu.Read(registers.HL); } //LD D (HL)
		void OP_58() { registers.E=registers.B; } //LD E B
		void OP_59() { registers.E=registers.C; } //LD E C
		void OP_5A() { registers.E=registers.D; } //LD E D
		void OP_5B() { registers.E=registers.E; } //LD E E
		void OP_5C() { registers.E=registers.H; } //LD E H
		void OP_5D() { registers.E=registers.L; } //LD E L
		void OP_5E() { registers.E=mmu.Read(registers.HL); } //LD E (HL)
		void OP_60() { registers.H=registers.B; } //LD H B
		void OP_61() { registers.H=registers.C; } //LD H C
		void OP_62() { registers.H=registers.D; } //LD H D
		void OP_63() { registers.H=registers.E; } //LD H E
		void OP_64() { registers.H=registers.H; } //LD H H
		void OP_65() { registers.H=registers.L; } //LD H L
		void OP_66() { registers.H=mmu.Read(registers.HL); } //LD H (HL)
		void OP_68() { registers.L=registers.B; } //LD L B
		void OP_69() { registers.L=registers.C; } //LD L C
		void OP_6A() { registers.L=registers.D; } //LD L D
		void OP_6B() { registers.L=registers.E; } //LD L E
		void OP_6C() { registers.L=registers.H; } //LD L H
		void OP_6D() { registers.L=registers.L; } //LD L L
		void OP_6E() { registers.L=mmu.Read(registers.HL); } //LD L (HL)
		void OP_70() { mmu.WriteW(registers.HL, registers.B); } //LD (HL) B
		void OP_71() { mmu.WriteW(registers.HL, registers.C); } //LD (HL) C
		void OP_72() { mmu.WriteW(registers.HL, registers.D); } //LD (HL) D
		void OP_73() { mmu.WriteW(registers.HL, registers.E); } //LD (HL) E
		void OP_74() { mmu.WriteW(registers.HL, registers.H); } //LD (HL) H
		void OP_75() { mmu.WriteW(registers.HL, registers.L); } //LD (HL) L
		void OP_36() { mmu.WriteW(registers.HL, mmu.Read(registers.PC++)); } //LD (HL) n

		//ld-a-n
		void OP_0A() { registers.A=mmu.Read(registers.BC); } //LD A (BC)
		void OP_1A() { registers.A=mmu.Read(registers.DE); } //LD A (DE)
		void OP_FA() { registers.A=mmu.Read(mmu.ReadW(registers.PC));registers.PC+=2; } //LD A (nn)
		void OP_3E() { registers.A=mmu.Read(registers.PC++); } //LD A #

		//ld-n-a
		void OP_47() { registers.B=registers.A; } //LD B A
		void OP_4F() { registers.C=registers.A; } //LD C A
		void OP_57() { registers.D=registers.A; } //LD D A
		void OP_5F() { registers.E=registers.A; } //LD E A
		void OP_67() { registers.H=registers.A; } //LD H A
		void OP_6F() { registers.L=registers.A; } //LD L A
		void OP_02() { mmu.WriteW(registers.BC, registers.A); } //LD (BC) A
		void OP_12() { mmu.WriteW(registers.DE, registers.A); } //LD (DE) A
		void OP_77() { mmu.WriteW(registers.HL, registers.A); } //LD (HL) A
		void OP_EA() { mmu.WriteW(registers.PC, registers.A);registers.PC+=2; } //LD (nn) A

		//ld-a-(c)
		void OP_F2() { registers.A=mmu.Read((UInt16)(0xFF00 + registers.C)); } //LD A,($FF00+C)

		//ld-(c)-a
		void OP_E2() { mmu.Write((UInt16)(0xFF00 + registers.C), registers.A); } //LD ($FF00+C),A

		//ld-a-(hld)
		void OP_3A() { registers.A=mmu.Read(registers.HL); registers.Dec_HL(); } //LD A,(HL-)

		//ld-(hld)-a
		void OP_32() { mmu.Write(registers.HL, registers.A); registers.Dec_HL(); } //LD (HL-), A

		//ld-a-(hli)
		void OP_2A() { registers.A=mmu.Read(registers.HL); registers.Inc_HL(); } //LD A,(HL+)

		//ld-(hli)-a
		void OP_22() { mmu.Write(registers.HL, registers.A); registers.Inc_HL(); } //LD (HL+), A

		//ldh-(n)-a
		void OP_E0() { mmu.Write((UInt16)(0xFF00 + mmu.Read(registers.PC++)), registers.A); } //LD ($FF00+n),A 

		//ldh-a-(n)
		void OP_F0() { registers.A = mmu.Read((UInt16)(0xFF00 + mmu.Read(registers.PC++))); } //LD A,($FF00+n)
	}

}
