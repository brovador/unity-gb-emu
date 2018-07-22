//Disable assignement to the same variable warning
#pragma warning disable 1717
//Disable comparission to the same variable warning
#pragma warning disable 1718

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace brovador.GBEmulator {

	public class CPU {

		public float clockSpeed = 4.16f * 1000000f; //4.16Mhz
		public MMU mmu;

		public bool halt { get; private set; }
		public bool stop { get; private set; }

		public bool ime { get; private set; }

		public struct Timers {
			public uint t;
			public uint lastOpCycles;
//			public uint m;
		}

		public struct Registers
		{
			public byte A, B, C, D, E, H, L, F;
			public UInt16 PC;
			public UInt16 SP;

			public UInt16 AF { 
				get { return (UInt16)(((UInt16)A << 8) + (UInt16)F); } 
				set { A = (byte)(value >> 8 & 0x00FF); F = (byte)(value & 0x00FF); }
			}
			public UInt16 BC { 
				get {return (UInt16)(((UInt16)B << 8) + (UInt16)C); } 
				set { B = (byte)(value >> 8 & 0x00FF); C = (byte)(value & 0x00FF); }
			}
			public UInt16 DE { 
				get { return (UInt16)(((UInt16)D << 8) + (UInt16)E); } 
				set { D = (byte)((value & 0xFF00) >> 8); E = (byte)(value & 0x00FF); }
			}
			public UInt16 HL { 
				get { return (UInt16)(((UInt16)H << 8) + (UInt16)L); } 
				set { H = (byte)(value >> 8 & 0x00FF); L = (byte)(value & 0x00FF); }
			}

			public bool flagZ {
				set { this.F = (byte)(value ? this.F | 0x80 : this.F & (~0x80)); }
				get { return ((this.F & 0x80) != 0x00); }
			}
			public bool flagN {
				set { this.F = (byte)(value ? this.F | 0x40 : this.F & (~0x40)); }
				get { return ((this.F & 0x40) != 0x00); }
			}
			public bool flagH {
				set { this.F = (byte)(value ? this.F | 0x20 : this.F & (~0x20)); }
				get { return ((this.F & 0x20) != 0x00); }
			}
			public bool flagC {
				set { this.F = (byte)(value ? this.F | 0x10 : this.F & (~0x10)); }
				get { return ((this.F & 0x10) != 0x00); }
			}
		}

		public Timers timers;
		public Registers registers;
			
		public byte[] opcodeCycles = {
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

		public byte[] opcodeCyclesCB = {
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8,
			8, 8, 8, 8, 8, 8, 16, 8, 8, 8, 8, 8, 8, 8, 16, 8
		};


		public System.Action[] operations;
		public System.Action[] cbOperations;

		public CPU(MMU mmu) {
			this.mmu = mmu;
			timers.t = 0;

			operations = new System.Action[] {
				OP_00,OP_01,OP_02,OP_03,OP_04,OP_05,OP_06,OP_07,OP_08,OP_09,OP_0A,OP_0B,OP_0C,OP_0D,OP_0E,OP_0F,
				OP_10,OP_11,OP_12,OP_13,OP_14,OP_15,OP_16,OP_17,OP_18,OP_19,OP_1A,OP_1B,OP_1C,OP_1D,OP_1E,OP_1F,
				OP_20,OP_21,OP_22,OP_23,OP_24,OP_25,OP_26,OP_27,OP_28,OP_29,OP_2A,OP_2B,OP_2C,OP_2D,OP_2E,OP_2F,
				OP_30,OP_31,OP_32,OP_33,OP_34,OP_35,OP_36,OP_37,OP_38,OP_39,OP_3A,OP_3B,OP_3C,OP_3D,OP_3E,OP_3F,
				OP_40,OP_41,OP_42,OP_43,OP_44,OP_45,OP_46,OP_47,OP_48,OP_49,OP_4A,OP_4B,OP_4C,OP_4D,OP_4E,OP_4F,
				OP_50,OP_51,OP_52,OP_53,OP_54,OP_55,OP_56,OP_57,OP_58,OP_59,OP_5A,OP_5B,OP_5C,OP_5D,OP_5E,OP_5F,
				OP_60,OP_61,OP_62,OP_63,OP_64,OP_65,OP_66,OP_67,OP_68,OP_69,OP_6A,OP_6B,OP_6C,OP_6D,OP_6E,OP_6F,
				OP_70,OP_71,OP_72,OP_73,OP_74,OP_75,OP_76,OP_77,OP_78,OP_79,OP_7A,OP_7B,OP_7C,OP_7D,OP_7E,OP_7F,
				OP_80,OP_81,OP_82,OP_83,OP_84,OP_85,OP_86,OP_87,OP_88,OP_89,OP_8A,OP_8B,OP_8C,OP_8D,OP_8E,OP_8F,
				OP_90,OP_91,OP_92,OP_93,OP_94,OP_95,OP_96,OP_97,OP_98,OP_99,OP_9A,OP_9B,OP_9C,OP_9D,OP_9E,OP_9F,
				OP_A0,OP_A1,OP_A2,OP_A3,OP_A4,OP_A5,OP_A6,OP_A7,OP_A8,OP_A9,OP_AA,OP_AB,OP_AC,OP_AD,OP_AE,OP_AF,
				OP_B0,OP_B1,OP_B2,OP_B3,OP_B4,OP_B5,OP_B6,OP_B7,OP_B8,OP_B9,OP_BA,OP_BB,OP_BC,OP_BD,OP_BE,OP_BF,
				OP_C0,OP_C1,OP_C2,OP_C3,OP_C4,OP_C5,OP_C6,OP_C7,OP_C8,OP_C9,OP_CA,OP_CB,OP_CC,OP_CD,OP_CE,OP_CF,
				OP_D0,OP_D1,OP_D2,OP_XX,OP_D4,OP_D5,OP_D6,OP_D7,OP_D8,OP_D9,OP_DA,OP_XX,OP_DC,OP_XX,OP_DE,OP_DF,
				OP_E0,OP_E1,OP_E2,OP_XX,OP_XX,OP_E5,OP_E6,OP_E7,OP_E8,OP_E9,OP_EA,OP_XX,OP_XX,OP_XX,OP_EE,OP_EF,
				OP_F0,OP_F1,OP_F2,OP_F3,OP_XX,OP_F5,OP_F6,OP_F7,OP_F8,OP_F9,OP_FA,OP_FB,OP_XX,OP_XX,OP_FE,OP_FF
			};

			cbOperations = new System.Action[] {
				CB_00,CB_01,CB_02,CB_03,CB_04,CB_05,CB_06,CB_07,CB_08,CB_09,CB_0A,CB_0B,CB_0C,CB_0D,CB_0E,CB_0F,
				CB_10,CB_11,CB_12,CB_13,CB_14,CB_15,CB_16,CB_17,CB_18,CB_19,CB_1A,CB_1B,CB_1C,CB_1D,CB_1E,CB_1F,
				CB_20,CB_21,CB_22,CB_23,CB_24,CB_25,CB_26,CB_27,CB_28,CB_29,CB_2A,CB_2B,CB_2C,CB_2D,CB_2E,CB_2F,
				CB_30,CB_31,CB_32,CB_33,CB_34,CB_35,CB_36,CB_37,CB_38,CB_39,CB_3A,CB_3B,CB_3C,CB_3D,CB_3E,CB_3F,
				CB_40,CB_41,CB_42,CB_43,CB_44,CB_45,CB_46,CB_47,CB_48,CB_49,CB_4A,CB_4B,CB_4C,CB_4D,CB_4E,CB_4F,
				CB_50,CB_51,CB_52,CB_53,CB_54,CB_55,CB_56,CB_57,CB_58,CB_59,CB_5A,CB_5B,CB_5C,CB_5D,CB_5E,CB_5F,
				CB_60,CB_61,CB_62,CB_63,CB_64,CB_65,CB_66,CB_67,CB_68,CB_69,CB_6A,CB_6B,CB_6C,CB_6D,CB_6E,CB_6F,
				CB_70,CB_71,CB_72,CB_73,CB_74,CB_75,CB_76,CB_77,CB_78,CB_79,CB_7A,CB_7B,CB_7C,CB_7D,CB_7E,CB_7F,
				CB_80,CB_81,CB_82,CB_83,CB_84,CB_85,CB_86,CB_87,CB_88,CB_89,CB_8A,CB_8B,CB_8C,CB_8D,CB_8E,CB_8F,
				CB_90,CB_91,CB_92,CB_93,CB_94,CB_95,CB_96,CB_97,CB_98,CB_99,CB_9A,CB_9B,CB_9C,CB_9D,CB_9E,CB_9F,
				CB_A0,CB_A1,CB_A2,CB_A3,CB_A4,CB_A5,CB_A6,CB_A7,CB_A8,CB_A9,CB_AA,CB_AB,CB_AC,CB_AD,CB_AE,CB_AF,
				CB_B0,CB_B1,CB_B2,CB_B3,CB_B4,CB_B5,CB_B6,CB_B7,CB_B8,CB_B9,CB_BA,CB_BB,CB_BC,CB_BD,CB_BE,CB_BF,
				CB_C0,CB_C1,CB_C2,CB_C3,CB_C4,CB_C5,CB_C6,CB_C7,CB_C8,CB_C9,CB_CA,CB_CB,CB_CC,CB_CD,CB_CE,CB_CF,
				CB_D0,CB_D1,CB_D2,CB_D3,CB_D4,CB_D5,CB_D6,CB_D7,CB_D8,CB_D9,CB_DA,CB_DB,CB_DC,CB_DD,CB_DE,CB_DF,
				CB_E0,CB_E1,CB_E2,CB_E3,CB_E4,CB_E5,CB_E6,CB_E7,CB_E8,CB_E9,CB_EA,CB_EB,CB_EC,CB_ED,CB_EE,CB_EF,
				CB_F0,CB_F1,CB_F2,CB_F3,CB_F4,CB_F5,CB_F6,CB_F7,CB_F8,CB_F9,CB_FA,CB_FB,CB_FC,CB_FD,CB_FE,CB_FF
			};
		}


		public void Step()
		{
			var op = mmu.Read(registers.PC++);
			operations[op]();
			timers.lastOpCycles = opcodeCycles[op];
			timers.t += timers.lastOpCycles;

//			if (ime && mmu.HasInterrupts) {
//				ime = false;
//				halt = false;
//				if (mmu.CheckInterrupt(MMU.InterruptType.VBlank)) {
//					mmu.ClearInterrupt(MMU.InterruptType.VBlank);
//					RST_40();
//				} else if (mmu.CheckInterrupt(MMU.InterruptType.LCDCStatus)) {
//					mmu.ClearInterrupt(MMU.InterruptType.LCDCStatus);
//					RST_48();
//				} else if (mmu.CheckInterrupt(MMU.InterruptType.TimerOverflow)) {
//					mmu.ClearInterrupt(MMU.InterruptType.TimerOverflow);
//					RST_50();
//				} else if (mmu.CheckInterrupt(MMU.InterruptType.SerialTransferCompletion)) {
//					mmu.ClearInterrupt(MMU.InterruptType.SerialTransferCompletion);
//					RST_58();
//				} else if (mmu.CheckInterrupt(MMU.InterruptType.HighToLowP10P13)) {
//					mmu.ClearInterrupt(MMU.InterruptType.HighToLowP10P13);
//					RST_60();
//				} else {
//					ime = true;
//				}
//			}
		}


		#region 8bit loads

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
		void OP_FA() { registers.A=mmu.Read(mmu.ReadW(registers.PC)); registers.PC+=2; } //LD A (nn)
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
		void OP_EA() { mmu.WriteW(registers.PC, registers.A); registers.PC+=2; } //LD (nn) A

		//ld-a-(c)
		void OP_F2() { registers.A=mmu.Read((UInt16)(0xFF00 + registers.C)); } //LD A,($FF00+C)

		//ld-(c)-a
		void OP_E2() { mmu.Write((UInt16)(0xFF00 + registers.C), registers.A); } //LD ($FF00+C),A

		//ld-a-(hld)
		void OP_3A() { registers.A=mmu.Read(registers.HL); registers.HL--; } //LD A,(HL-)

		//ld-(hld)-a
		void OP_32() { mmu.Write(registers.HL, registers.A); registers.HL--; } //LD (HL-), A

		//ld-a-(hli)
		void OP_2A() { registers.A=mmu.Read(registers.HL); registers.HL++; } //LD A,(HL+)

		//ld-(hli)-a
		void OP_22() { mmu.Write(registers.HL, registers.A); registers.HL++; } //LD (HL+), A

		//ldh-(n)-a
		void OP_E0() { mmu.Write((UInt16)(0xFF00 + mmu.Read(registers.PC++)), registers.A); } //LD ($FF00+n),A 

		//ldh-a-(n)
		void OP_F0() { registers.A = mmu.Read((UInt16)(0xFF00 + mmu.Read(registers.PC++))); } //LD A,($FF00+n)

		#endregion

		#region 16bit loads

		//ld-n-nn
		void OP_01() { registers.BC=mmu.ReadW(registers.PC); registers.PC+=2; } //LD BC,nn
		void OP_11() { registers.DE=mmu.ReadW(registers.PC); registers.PC+=2; } //LD DE,nn
		void OP_21() { registers.HL=mmu.ReadW(registers.PC); registers.PC+=2; } //LD HL,nn
		void OP_31() { registers.SP=mmu.ReadW(registers.PC); registers.PC+=2; } //LD SP,nn

		//ld-sp-hl
		void OP_F9() { registers.SP=registers.HL; } //LD SP,HL

		//ldhl-sp-n
		#warning set flags carry and half-carry? (jsGB doesn't do it)
		void OP_F8() { registers.HL=(UInt16)(registers.SP+DecodeSigned(mmu.Read(registers.PC++))); registers.flagZ=false; registers.flagN=false; } //LDHL SP,n 

		//ld-nn-sp
		void OP_08() { mmu.WriteW(mmu.ReadW(registers.PC), registers.SP); registers.PC+=2; } //LD (nn),SP

		//push-nn
		void OP_F5() { registers.SP-=2; mmu.WriteW(registers.SP,registers.AF); }// PUSH AF
		void OP_C5() { registers.SP-=2; mmu.WriteW(registers.SP,registers.BC); }// PUSH BC
		void OP_D5() { registers.SP-=2; mmu.WriteW(registers.SP,registers.DE); }// PUSH DE
		void OP_E5() { registers.SP-=2; mmu.WriteW(registers.SP,registers.HL); }// PUSH HL

		//pop-nn
		void OP_F1() { registers.AF=mmu.ReadW(registers.SP); registers.SP+=2; }  //POP AF
		void OP_C1() { registers.BC=mmu.ReadW(registers.SP); registers.SP+=2; }  //POP BC
		void OP_D1() { registers.DE=mmu.ReadW(registers.SP); registers.SP+=2; }  //POP DE
		void OP_E1() { registers.HL=mmu.ReadW(registers.SP); registers.SP+=2; }  //POP HL

		#endregion

		#region 8-bit ALU

		//add
		void OP_87() { byte tmp=registers.A; registers.A+=registers.A; registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A A
		void OP_80() { byte tmp=registers.A; registers.A+=registers.B; registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A B
		void OP_81() { byte tmp=registers.A; registers.A+=registers.C; registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A C
		void OP_82() { byte tmp=registers.A; registers.A+=registers.D; registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A D
		void OP_83() { byte tmp=registers.A; registers.A+=registers.E; registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A E
		void OP_84() { byte tmp=registers.A; registers.A+=registers.H; registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A H
		void OP_85() { byte tmp=registers.A; registers.A+=registers.L; registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A L
		void OP_86() { byte tmp=registers.A; registers.A+=mmu.Read(registers.HL); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A (HL)
		void OP_C6() { byte tmp=registers.A; registers.A+=mmu.Read(registers.PC++); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A #

		//adc
		void OP_8F() { byte tmp=registers.A; registers.A+=(byte)(registers.A+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A A
		void OP_88() { byte tmp=registers.A; registers.A+=(byte)(registers.B+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A B
		void OP_89() { byte tmp=registers.A; registers.A+=(byte)(registers.C+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A C
		void OP_8A() { byte tmp=registers.A; registers.A+=(byte)(registers.D+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A D
		void OP_8B() { byte tmp=registers.A; registers.A+=(byte)(registers.E+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A E
		void OP_8C() { byte tmp=registers.A; registers.A+=(byte)(registers.H+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A H
		void OP_8D() { byte tmp=registers.A; registers.A+=(byte)(registers.L+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A L
		void OP_8E() { byte tmp=registers.A; registers.A+=(byte)(mmu.Read(registers.HL)+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A (HL)
		void OP_CE() { byte tmp=registers.A; registers.A+=(byte)(mmu.Read(registers.PC++)+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=((tmp&0x0F)>(registers.A&0x0F)); registers.flagC=(tmp>registers.A); } //LD A #

		//sub
		void OP_97() { byte tmp=registers.A; registers.A-=registers.A; registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB A
		void OP_90() { byte tmp=registers.A; registers.A-=registers.B; registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB B
		void OP_91() { byte tmp=registers.A; registers.A-=registers.C; registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB C
		void OP_92() { byte tmp=registers.A; registers.A-=registers.D; registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB D
		void OP_93() { byte tmp=registers.A; registers.A-=registers.E; registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB E
		void OP_94() { byte tmp=registers.A; registers.A-=registers.H; registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB H
		void OP_95() { byte tmp=registers.A; registers.A-=registers.L; registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB L
		void OP_96() { byte tmp=registers.A; registers.A-=mmu.Read(registers.HL); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB (HL)
		void OP_D6() { byte tmp=registers.A; registers.A-=mmu.Read(registers.PC++); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SUB #

		//sbc
		void OP_9F() { byte tmp=registers.A; registers.A-=(byte)(registers.A+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC A
		void OP_98() { byte tmp=registers.A; registers.A-=(byte)(registers.B+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC B
		void OP_99() { byte tmp=registers.A; registers.A-=(byte)(registers.C+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC C
		void OP_9A() { byte tmp=registers.A; registers.A-=(byte)(registers.D+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC D
		void OP_9B() { byte tmp=registers.A; registers.A-=(byte)(registers.E+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC E
		void OP_9C() { byte tmp=registers.A; registers.A-=(byte)(registers.H+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC H
		void OP_9D() { byte tmp=registers.A; registers.A-=(byte)(registers.L+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC L
		void OP_9E() { byte tmp=registers.A; registers.A-=(byte)(mmu.Read(registers.HL)+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC (HL)
		void OP_DE() { byte tmp=registers.A; registers.A-=(byte)(mmu.Read(registers.PC++)+(registers.flagC?1:0)); registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=((tmp&0x0F)<(registers.A&0x0F)); registers.flagC=(tmp<registers.A); } //SBC #

		//and-n
		void OP_A7() { registers.A=(byte)(registers.A&registers.A); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND A
		void OP_A0() { registers.A=(byte)(registers.A&registers.B); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND B
		void OP_A1() { registers.A=(byte)(registers.A&registers.C); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND C
		void OP_A2() { registers.A=(byte)(registers.A&registers.D); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND D
		void OP_A3() { registers.A=(byte)(registers.A&registers.E); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND E
		void OP_A4() { registers.A=(byte)(registers.A&registers.H); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND H
		void OP_A5() { registers.A=(byte)(registers.A&registers.L); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND L
		void OP_A6() { registers.A=(byte)(registers.A&mmu.Read(registers.HL)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND (HL)
		void OP_E6() { registers.A=(byte)(registers.A&mmu.Read(registers.PC++)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=true; registers.flagC=false; } //AND #

		//or-n
		void OP_B7() { registers.A=(byte)(registers.A|registers.A); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR A
		void OP_B0() { registers.A=(byte)(registers.A|registers.B); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR B
		void OP_B1() { registers.A=(byte)(registers.A|registers.C); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR C
		void OP_B2() { registers.A=(byte)(registers.A|registers.D); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR D
		void OP_B3() { registers.A=(byte)(registers.A|registers.E); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR E
		void OP_B4() { registers.A=(byte)(registers.A|registers.H); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR H
		void OP_B5() { registers.A=(byte)(registers.A|registers.L); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR L
		void OP_B6() { registers.A=(byte)(registers.A|mmu.Read(registers.HL)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR (HL)
		void OP_F6() { registers.A=(byte)(registers.A|mmu.Read(registers.PC++)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //OR #

		//xor-n
		void OP_AF() { registers.A=(byte)(registers.A^registers.A); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR A
		void OP_A8() { registers.A=(byte)(registers.A^registers.B); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR B
		void OP_A9() { registers.A=(byte)(registers.A^registers.C); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR C
		void OP_AA() { registers.A=(byte)(registers.A^registers.D); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR D
		void OP_AB() { registers.A=(byte)(registers.A^registers.E); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR E
		void OP_AC() { registers.A=(byte)(registers.A^registers.H); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR H
		void OP_AD() { registers.A=(byte)(registers.A^registers.L); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR L
		void OP_AE() { registers.A=(byte)(registers.A^mmu.Read(registers.HL)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR (HL)
		void OP_EE() { registers.A=(byte)(registers.A^mmu.Read(registers.PC++)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; } //XOR #

		//cp-n
		void OP_BF() { registers.flagZ=(registers.A==registers.A); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(registers.A&0x0F)); registers.flagC=(registers.A<registers.A); } //CP A
		void OP_B8() { registers.flagZ=(registers.A==registers.B); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(registers.B&0x0F)); registers.flagC=(registers.A<registers.B); } //CP B
		void OP_B9() { registers.flagZ=(registers.A==registers.C); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(registers.C&0x0F)); registers.flagC=(registers.A<registers.C); } //CP C
		void OP_BA() { registers.flagZ=(registers.A==registers.D); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(registers.D&0x0F)); registers.flagC=(registers.A<registers.D); } //CP D
		void OP_BB() { registers.flagZ=(registers.A==registers.E); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(registers.E&0x0F)); registers.flagC=(registers.A<registers.E); } //CP E
		void OP_BC() { registers.flagZ=(registers.A==registers.H); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(registers.H&0x0F)); registers.flagC=(registers.A<registers.H); } //CP H
		void OP_BD() { registers.flagZ=(registers.A==registers.L); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(registers.L&0x0F)); registers.flagC=(registers.A<registers.L); } //CP L
		void OP_BE() { registers.flagZ=(registers.A==mmu.Read(registers.HL)); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(mmu.Read(registers.HL)&0x0F)); registers.flagC=(registers.A<mmu.Read(registers.HL)); } //CP (HL)
		void OP_FE() { byte tmp=mmu.Read(registers.PC++); registers.flagZ=(registers.A==tmp); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(tmp&0x0F)); registers.flagC=(registers.A<tmp); } //CP #

		//inc-n
		void OP_3C() { registers.A++; registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=(((registers.A-1)&0x0F)>(registers.A&0x0F)); } //INC A
		void OP_04() { registers.B++; registers.flagZ=(registers.B==0); registers.flagN=false; registers.flagH=(((registers.B-1)&0x0F)>(registers.B&0x0F)); } //INC B
		void OP_0C() { registers.C++; registers.flagZ=(registers.C==0); registers.flagN=false; registers.flagH=(((registers.C-1)&0x0F)>(registers.C&0x0F)); } //INC C
		void OP_14() { registers.D++; registers.flagZ=(registers.D==0); registers.flagN=false; registers.flagH=(((registers.D-1)&0x0F)>(registers.D&0x0F)); } //INC D
		void OP_1C() { registers.E++; registers.flagZ=(registers.E==0); registers.flagN=false; registers.flagH=(((registers.E-1)&0x0F)>(registers.E&0x0F)); } //INC E
		void OP_24() { registers.H++; registers.flagZ=(registers.H==0); registers.flagN=false; registers.flagH=(((registers.H-1)&0x0F)>(registers.H&0x0F)); } //INC H
		void OP_2C() { registers.L++; registers.flagZ=(registers.L==0); registers.flagN=false; registers.flagH=(((registers.L-1)&0x0F)>(registers.L&0x0F)); } //INC L
		void OP_34() { byte tmp=(byte)(mmu.Read(registers.HL)+1); mmu.Write(registers.HL,tmp); registers.flagZ=(tmp==0); registers.flagN=false; registers.flagH=(((tmp-1)&0x0F)>(tmp&0x0F)); } //INC (HL)

		//dec-n
		void OP_3D() { registers.A--; registers.flagZ=(registers.A==0); registers.flagN=true; registers.flagH=(((registers.A+1)&0x0F)<(registers.A&0x0F)); } //DEC A
		void OP_05() { registers.B--; registers.flagZ=(registers.B==0); registers.flagN=true; registers.flagH=(((registers.B+1)&0x0F)<(registers.B&0x0F)); } //DEC B
		void OP_0D() { registers.C--; registers.flagZ=(registers.C==0); registers.flagN=true; registers.flagH=(((registers.C+1)&0x0F)<(registers.C&0x0F)); } //DEC C
		void OP_15() { registers.D--; registers.flagZ=(registers.D==0); registers.flagN=true; registers.flagH=(((registers.D+1)&0x0F)<(registers.D&0x0F)); } //DEC D
		void OP_1D() { registers.E--; registers.flagZ=(registers.E==0); registers.flagN=true; registers.flagH=(((registers.E+1)&0x0F)<(registers.E&0x0F)); } //DEC E
		void OP_25() { registers.H--; registers.flagZ=(registers.H==0); registers.flagN=true; registers.flagH=(((registers.H+1)&0x0F)<(registers.H&0x0F)); } //DEC H
		void OP_2D() { registers.L--; registers.flagZ=(registers.L==0); registers.flagN=true; registers.flagH=(((registers.L+1)&0x0F)<(registers.L&0x0F)); } //DEC L
		void OP_35() { byte tmp=(byte)(mmu.Read(registers.HL)-1); mmu.Write(registers.HL,tmp); registers.flagZ=(tmp==0); registers.flagN=true; registers.flagH=(((tmp+1)&0x0F)<(tmp&0x0F)); } //DEC (HL)


		#endregion

		#region 16-bit ALU

		//add-hl-n
		void OP_09() { UInt16 tmp=registers.HL; registers.HL+=registers.BC; registers.flagN=false; registers.flagH=((registers.H&0x0F)<(((tmp&0xFF00)>>8)&0x0F)); registers.flagC=(tmp>registers.HL); } //ADD HL BC
		void OP_19() { UInt16 tmp=registers.HL; registers.HL+=registers.DE; registers.flagN=false; registers.flagH=((registers.H&0x0F)<(((tmp&0xFF00)>>8)&0x0F)); registers.flagC=(tmp>registers.HL); } //ADD HL DE
		void OP_29() { UInt16 tmp=registers.HL; registers.HL+=registers.HL; registers.flagN=false; registers.flagH=((registers.H&0x0F)<(((tmp&0xFF00)>>8)&0x0F)); registers.flagC=(tmp>registers.HL); } //ADD HL HL
		void OP_39() { UInt16 tmp=registers.HL; registers.HL+=registers.SP; registers.flagN=false; registers.flagH=((registers.H&0x0F)<(((tmp&0xFF00)>>8)&0x0F)); registers.flagC=(tmp>registers.HL); } //ADD HL SP

		//add-sp-n
		#warning set flags carry and half-carry? (jsGB doesn't do it)
		void OP_E8() { registers.SP=(UInt16)(registers.SP+DecodeSigned(mmu.Read(registers.PC++))); registers.flagZ=false; registers.flagN=false; }

		//inc-nn
		void OP_03() { registers.BC++; } //INC BC
		void OP_13() { registers.DE++; } //INC DE
		void OP_23() { registers.HL++; } //INC HL
		void OP_33() { registers.SP++; } //INC SP

		//dec-nn
		void OP_0B() { registers.BC--; } //DEC BC
		void OP_1B() { registers.DE--; } //DEC DE
		void OP_2B() { registers.HL--; } //DEC HL
		void OP_3B() { registers.SP--; } //DEC SP

		#endregion

		#region misc functions

		//DAA
		#warning review this one
		void OP_27() {
			registers.flagC = false;
			registers.flagH = false;
			if ((0x0F & registers.A) > 9 || registers.flagH)
				registers.A += 0x06;
			if (((0xF0 & registers.A) >> 4) > 9 || registers.flagC) {
				registers.A += 0x60;
				registers.flagC = true;
			}
			registers.flagZ = (registers.A == 0);
		}

		//cpl
		void OP_2F() { registers.A=(byte)(~registers.A); registers.flagN=true; registers.flagH=true; }

		//ccf
		void OP_3F() { registers.flagC=!registers.flagC; registers.flagN=false; registers.flagH=false; }

		//scf
		void OP_37() { registers.flagC=true; registers.flagN=false; registers.flagH=false; }

		//nop
		void OP_00() {}

		//halt
		void OP_76() { halt = true; }

		//stop
		void OP_10() { stop = true; }

		//di
		void OP_F3() { ime = false; }

		//ei
		void OP_FB() { ime = true; }

		#endregion

		#region Rotates & shifts

		//rlca
		void OP_07() { 
			registers.flagC = ((registers.A & 0x80) != 0); 
			registers.A = (byte)((registers.A << 1) | ((registers.flagC?0x00:0x01) >> 7)); 
			registers.flagZ = (registers.A == 0); 
			registers.flagH = false; 
			registers.flagN = false; 
		}

		//rla
		void OP_17() {
			bool flagC = registers.flagC;
			registers.flagC = ((registers.A >> 7) != 0); 
			registers.A = (byte)((registers.A << 1) | (flagC?0x01:0x00));
			registers.flagZ = (registers.A == 0); 
			registers.flagH = false; 
			registers.flagN = false; 
		}


		//rrca
		void OP_0F() { 
			registers.flagC = ((registers.A & 0x01) != 0); 
			registers.A = (byte)((registers.A >> 1) | ((registers.flagC?0x01:0x00) << 7)); 
			registers.flagZ = (registers.A == 0); 
			registers.flagH = false; 
			registers.flagN = false; 
		}

		//rra
		void OP_1F() {
			bool flagC = registers.flagC;
			registers.flagC = ((registers.A << 7) != 0); 
			registers.A = (byte)((registers.A >> 1) | (flagC?0x80:0x00));
			registers.flagZ = (registers.A == 0); 
			registers.flagH = false; 
			registers.flagN = false; 
		}
		
		#endregion

		#region Jumps

		//jp nn
		void OP_C3() { registers.PC = mmu.ReadW(registers.PC); }

		//jp cc,nn
		void OP_C2() { if (!registers.flagZ) { registers.PC=mmu.ReadW(registers.PC); } else { registers.PC+=2; } }
		void OP_CA() { if (registers.flagZ) { registers.PC=mmu.ReadW(registers.PC); } else { registers.PC+=2; } }
		void OP_D2() { if (!registers.flagC) { registers.PC=mmu.ReadW(registers.PC); } else { registers.PC+=2; } }
		void OP_DA() { if (registers.flagC) { registers.PC=mmu.ReadW(registers.PC); } else { registers.PC+=2; } }

		//jp hl
		void OP_E9() { registers.PC = registers.HL; }

		//jr n
		void OP_18() { var val = DecodeSigned(mmu.Read(registers.PC++)); registers.PC = (UInt16)(registers.PC+val); }

		//jr cc,n
		void OP_20() { if (!registers.flagZ) { var val = DecodeSigned(mmu.Read(registers.PC++)); registers.PC = (UInt16)(registers.PC+val); } else { registers.PC++; } }
		void OP_28() { if (registers.flagZ) { var val = DecodeSigned(mmu.Read(registers.PC++)); registers.PC = (UInt16)(registers.PC+val); } else { registers.PC++; } }
		void OP_30() { if (!registers.flagC) { var val = DecodeSigned(mmu.Read(registers.PC++)); registers.PC = (UInt16)(registers.PC+val); } else { registers.PC++; } }
		void OP_38() { if (registers.flagC) { var val = DecodeSigned(mmu.Read(registers.PC++)); registers.PC = (UInt16)(registers.PC+val); } else { registers.PC++; } }

		#endregion

		#region Calls

		//call nn
		void OP_CD() { registers.SP -= 2; mmu.WriteW(registers.SP, (UInt16)(registers.PC+2)); registers.PC=mmu.ReadW(registers.PC); }

		//call cc,nn
		void OP_C4() { if (!registers.flagZ) { registers.SP -= 2; mmu.WriteW(registers.SP, (UInt16)(registers.PC+2)); registers.PC=mmu.ReadW(registers.PC); } else { registers.PC+=2; } }
		void OP_CC() { if (registers.flagZ) { registers.SP -= 2; mmu.WriteW(registers.SP, (UInt16)(registers.PC+2)); registers.PC=mmu.ReadW(registers.PC); } else { registers.PC+=2; } }
		void OP_D4() { if (!registers.flagC) { registers.SP -= 2; mmu.WriteW(registers.SP, (UInt16)(registers.PC+2)); registers.PC=mmu.ReadW(registers.PC); } else { registers.PC+=2; } }
		void OP_DC() { if (registers.flagC) { registers.SP -= 2; mmu.WriteW(registers.SP, (UInt16)(registers.PC+2)); registers.PC=mmu.ReadW(registers.PC); } else { registers.PC+=2; } }

		#endregion

		#region Restarts & returns

		//rst
		#warning jsGB saves here all the registers and rstore them on RETI
		void OP_C7() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x00; } //RST 00H
		void OP_CF() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x08; } //RST 08H
		void OP_D7() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x10; } //RST 10H
		void OP_DF() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x18; } //RST 18H
		void OP_E7() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x20; } //RST 20H
		void OP_EF() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x28; } //RST 28H
		void OP_F7() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x30; } //RST 30H
		void OP_FF() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x38; } //RST 38H

		void RST_40() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x40; }
		void RST_48() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x48; }
		void RST_50() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x50; }
		void RST_58() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x58; }
		void RST_60() { registers.SP -= 2; mmu.WriteW(registers.SP, registers.PC); registers.PC=0x60; }

		//ret
		void OP_C9() { registers.PC=mmu.ReadW(registers.SP); registers.SP+=2; }

		//ret cc
		void OP_C0() { if (!registers.flagZ) { registers.PC=mmu.ReadW(registers.SP); registers.SP+=2; } }
		void OP_C8() { if (registers.flagZ) { registers.PC=mmu.ReadW(registers.SP); registers.SP+=2; } }
		void OP_D0() { if (!registers.flagC) { registers.PC=mmu.ReadW(registers.SP); registers.SP+=2; } }
		void OP_D8() { if (registers.flagC) { registers.PC=mmu.ReadW(registers.SP); registers.SP+=2; } }

		//ret
		#warning jsGB restores all the registers here
		void OP_D9() { registers.PC=mmu.ReadW(registers.SP); registers.SP+=2; ime = true; }

		#endregion

		#region CB operations

		#warning review if cycles need to be adjusted (CB has 4 + instruction ones)
		void OP_CB() {
			var op = mmu.Read(registers.PC++);
			cbOperations[op]();
			timers.t += opcodeCyclesCB[op];
		}

		//swap
		void CB_37() { byte tmp=registers.A; registers.A = (byte)(((tmp&0x0F)<<4)|((tmp&0xF0)>>4)); registers.flagZ=(registers.A==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; }
		void CB_30() { byte tmp=registers.B; registers.B = (byte)(((tmp&0x0F)<<4)|((tmp&0xF0)>>4)); registers.flagZ=(registers.B==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; }
		void CB_31() { byte tmp=registers.C; registers.C = (byte)(((tmp&0x0F)<<4)|((tmp&0xF0)>>4)); registers.flagZ=(registers.C==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; }
		void CB_32() { byte tmp=registers.D; registers.D = (byte)(((tmp&0x0F)<<4)|((tmp&0xF0)>>4)); registers.flagZ=(registers.D==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; }
		void CB_33() { byte tmp=registers.E; registers.E = (byte)(((tmp&0x0F)<<4)|((tmp&0xF0)>>4)); registers.flagZ=(registers.E==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; }
		void CB_34() { byte tmp=registers.H; registers.H = (byte)(((tmp&0x0F)<<4)|((tmp&0xF0)>>4)); registers.flagZ=(registers.H==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; }
		void CB_35() { byte tmp=registers.L; registers.L = (byte)(((tmp&0x0F)<<4)|((tmp&0xF0)>>4)); registers.flagZ=(registers.L==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; }
		void CB_36() { byte tmp=mmu.Read(registers.HL); mmu.Write(registers.HL,(byte)(((tmp&0x0F)<<4)|((tmp&0xF0)>>4))); registers.flagZ=(mmu.Read(registers.HL)==0); registers.flagN=false; registers.flagH=false; registers.flagC=false; }

		//rlc
		void CB_07() { registers.flagC=((registers.A & 0x80)!=0);  registers.A=(byte)((registers.A<<1)|((registers.flagC?0x00:0x01)>>7));  registers.flagZ=(registers.A==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_00() { registers.flagC=((registers.B & 0x80)!=0);  registers.B=(byte)((registers.B<<1)|((registers.flagC?0x00:0x01)>>7));  registers.flagZ=(registers.B==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_01() { registers.flagC=((registers.C & 0x80)!=0);  registers.C=(byte)((registers.C<<1)|((registers.flagC?0x00:0x01)>>7));  registers.flagZ=(registers.C==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_02() { registers.flagC=((registers.D & 0x80)!=0);  registers.D=(byte)((registers.D<<1)|((registers.flagC?0x00:0x01)>>7));  registers.flagZ=(registers.D==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_03() { registers.flagC=((registers.E & 0x80)!=0);  registers.E=(byte)((registers.E<<1)|((registers.flagC?0x00:0x01)>>7));  registers.flagZ=(registers.E==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_04() { registers.flagC=((registers.H & 0x80)!=0);  registers.H=(byte)((registers.H<<1)|((registers.flagC?0x00:0x01)>>7));  registers.flagZ=(registers.H==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_05() { registers.flagC=((registers.L & 0x80)!=0);  registers.L=(byte)((registers.L<<1)|((registers.flagC?0x00:0x01)>>7));  registers.flagZ=(registers.L==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_06() { registers.flagC=((mmu.Read(registers.HL) & 0x80)!=0);  mmu.Write(registers.HL, (byte)((mmu.Read(registers.HL)<<1)|((registers.flagC?0x00:0x01)>>7)));  registers.flagZ=(mmu.Read(registers.HL)==0);  registers.flagH=false;  registers.flagN=false; }


		//rl
		void CB_17() { bool flagC=registers.flagC; registers.flagC=((registers.A>>7)!= 0);  registers.A=(byte)((registers.A << 1)|(flagC?0x01:0x00)); registers.flagZ=(registers.A==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_10() { bool flagC=registers.flagC; registers.flagC=((registers.B>>7)!= 0);  registers.B=(byte)((registers.B << 1)|(flagC?0x01:0x00)); registers.flagZ=(registers.B==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_11() { bool flagC=registers.flagC; registers.flagC=((registers.C>>7)!= 0);  registers.C=(byte)((registers.C << 1)|(flagC?0x01:0x00)); registers.flagZ=(registers.C==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_12() { bool flagC=registers.flagC; registers.flagC=((registers.D>>7)!= 0);  registers.D=(byte)((registers.D << 1)|(flagC?0x01:0x00)); registers.flagZ=(registers.D==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_13() { bool flagC=registers.flagC; registers.flagC=((registers.E>>7)!= 0);  registers.E=(byte)((registers.E << 1)|(flagC?0x01:0x00)); registers.flagZ=(registers.E==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_14() { bool flagC=registers.flagC; registers.flagC=((registers.H>>7)!= 0);  registers.H=(byte)((registers.H << 1)|(flagC?0x01:0x00)); registers.flagZ=(registers.H==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_15() { bool flagC=registers.flagC; registers.flagC=((registers.L>>7)!= 0);  registers.L=(byte)((registers.L << 1)|(flagC?0x01:0x00)); registers.flagZ=(registers.L==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_16() { bool flagC=registers.flagC; registers.flagC=((mmu.Read(registers.HL)>>7)!= 0);  mmu.Write(registers.HL, (byte)((mmu.Read(registers.HL) << 1)|(flagC?0x01:0x00))); registers.flagZ=(mmu.Read(registers.HL)==0);  registers.flagH=false;  registers.flagN=false; }


		//rrc
		void CB_0F() { registers.flagC=((registers.A & 0x01)!=0);  registers.A=(byte)((registers.A>>1)|((registers.flagC?0x01:0x00)<<7));  registers.flagZ=(registers.A==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_08() { registers.flagC=((registers.B & 0x01)!=0);  registers.B=(byte)((registers.B>>1)|((registers.flagC?0x01:0x00)<<7));  registers.flagZ=(registers.B==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_09() { registers.flagC=((registers.C & 0x01)!=0);  registers.C=(byte)((registers.C>>1)|((registers.flagC?0x01:0x00)<<7));  registers.flagZ=(registers.C==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_0A() { registers.flagC=((registers.D & 0x01)!=0);  registers.D=(byte)((registers.D>>1)|((registers.flagC?0x01:0x00)<<7));  registers.flagZ=(registers.D==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_0B() { registers.flagC=((registers.E & 0x01)!=0);  registers.E=(byte)((registers.E>>1)|((registers.flagC?0x01:0x00)<<7));  registers.flagZ=(registers.E==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_0C() { registers.flagC=((registers.H & 0x01)!=0);  registers.H=(byte)((registers.H>>1)|((registers.flagC?0x01:0x00)<<7));  registers.flagZ=(registers.H==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_0D() { registers.flagC=((registers.L & 0x01)!=0);  registers.L=(byte)((registers.L>>1)|((registers.flagC?0x01:0x00)<<7));  registers.flagZ=(registers.L==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_0E() { registers.flagC=((mmu.Read(registers.HL) & 0x01)!=0);  mmu.Write(registers.HL, (byte)((mmu.Read(registers.HL)>>1)|((registers.flagC?0x01:0x00)<<7)));  registers.flagZ=(mmu.Read(registers.HL)==0);  registers.flagH=false;  registers.flagN=false; }


		//rr
		void CB_1F() { bool flagC=registers.flagC; registers.flagC=((registers.A<<7)!=0);  registers.A=(byte)((registers.A>>1)|(flagC?0x80:0x00)); registers.flagZ=(registers.A==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_18() { bool flagC=registers.flagC; registers.flagC=((registers.B<<7)!=0);  registers.B=(byte)((registers.B>>1)|(flagC?0x80:0x00)); registers.flagZ=(registers.B==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_19() { bool flagC=registers.flagC; registers.flagC=((registers.C<<7)!=0);  registers.C=(byte)((registers.C>>1)|(flagC?0x80:0x00)); registers.flagZ=(registers.C==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_1A() { bool flagC=registers.flagC; registers.flagC=((registers.D<<7)!=0);  registers.D=(byte)((registers.D>>1)|(flagC?0x80:0x00)); registers.flagZ=(registers.D==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_1B() { bool flagC=registers.flagC; registers.flagC=((registers.E<<7)!=0);  registers.E=(byte)((registers.E>>1)|(flagC?0x80:0x00)); registers.flagZ=(registers.E==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_1C() { bool flagC=registers.flagC; registers.flagC=((registers.H<<7)!=0);  registers.H=(byte)((registers.H>>1)|(flagC?0x80:0x00)); registers.flagZ=(registers.H==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_1D() { bool flagC=registers.flagC; registers.flagC=((registers.L<<7)!=0);  registers.L=(byte)((registers.L>>1)|(flagC?0x80:0x00)); registers.flagZ=(registers.L==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_1E() { bool flagC=registers.flagC; registers.flagC=((mmu.Read(registers.HL)<<7)!=0);  mmu.Write(registers.HL,(byte)((mmu.Read(registers.HL)>>1)|(flagC?0x80:0x00))); registers.flagZ=(mmu.Read(registers.HL)==0);  registers.flagH=false;  registers.flagN=false; }

		//sla
		void CB_27() { registers.flagC=((registers.A & 0x80)!=0);  registers.A=(byte)(registers.A<<1);  registers.flagZ=(registers.A==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_20() { registers.flagC=((registers.B & 0x80)!=0);  registers.B=(byte)(registers.B<<1);  registers.flagZ=(registers.B==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_21() { registers.flagC=((registers.C & 0x80)!=0);  registers.C=(byte)(registers.C<<1);  registers.flagZ=(registers.C==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_22() { registers.flagC=((registers.D & 0x80)!=0);  registers.D=(byte)(registers.D<<1);  registers.flagZ=(registers.D==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_23() { registers.flagC=((registers.E & 0x80)!=0);  registers.E=(byte)(registers.E<<1);  registers.flagZ=(registers.E==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_24() { registers.flagC=((registers.H & 0x80)!=0);  registers.H=(byte)(registers.H<<1);  registers.flagZ=(registers.H==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_25() { registers.flagC=((registers.L & 0x80)!=0);  registers.L=(byte)(registers.L<<1);  registers.flagZ=(registers.L==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_26() { registers.flagC=((mmu.Read(registers.HL) & 0x80)!=0);  mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)<<1));  registers.flagZ=(mmu.Read(registers.HL)==0);  registers.flagH=false;  registers.flagN=false; }


		//sra
		void CB_2F() { byte tmp=(byte)(registers.A&0x80); registers.flagC=((registers.A&0x01)!=0);  registers.A=(byte)((registers.A>>1)+tmp);  registers.flagZ=(registers.A==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_28() { byte tmp=(byte)(registers.B&0x80); registers.flagC=((registers.B&0x01)!=0);  registers.B=(byte)((registers.B>>1)+tmp);  registers.flagZ=(registers.B==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_29() { byte tmp=(byte)(registers.C&0x80); registers.flagC=((registers.C&0x01)!=0);  registers.C=(byte)((registers.C>>1)+tmp);  registers.flagZ=(registers.C==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_2A() { byte tmp=(byte)(registers.D&0x80); registers.flagC=((registers.D&0x01)!=0);  registers.D=(byte)((registers.D>>1)+tmp);  registers.flagZ=(registers.D==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_2B() { byte tmp=(byte)(registers.E&0x80); registers.flagC=((registers.E&0x01)!=0);  registers.E=(byte)((registers.E>>1)+tmp);  registers.flagZ=(registers.E==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_2C() { byte tmp=(byte)(registers.H&0x80); registers.flagC=((registers.H&0x01)!=0);  registers.H=(byte)((registers.H>>1)+tmp);  registers.flagZ=(registers.H==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_2D() { byte tmp=(byte)(registers.L&0x80); registers.flagC=((registers.L&0x01)!=0);  registers.L=(byte)((registers.L>>1)+tmp);  registers.flagZ=(registers.L==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_2E() { byte tmp=(byte)(mmu.Read(registers.HL)&0x80); registers.flagC=((mmu.Read(registers.HL)&0x01)!=0);  mmu.Write(registers.HL,(byte)((mmu.Read(registers.HL)>>1)+tmp));  registers.flagZ=(mmu.Read(registers.HL)==0);  registers.flagH=false;  registers.flagN=false; }


		//srl
		void CB_3F() { registers.flagC=((registers.A&0x01)!=0);  registers.A=(byte)(registers.A>>1);  registers.flagZ=(registers.A==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_38() { registers.flagC=((registers.B&0x01)!=0);  registers.B=(byte)(registers.B>>1);  registers.flagZ=(registers.B==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_39() { registers.flagC=((registers.C&0x01)!=0);  registers.C=(byte)(registers.C>>1);  registers.flagZ=(registers.C==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_3A() { registers.flagC=((registers.D&0x01)!=0);  registers.D=(byte)(registers.D>>1);  registers.flagZ=(registers.D==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_3B() { registers.flagC=((registers.E&0x01)!=0);  registers.E=(byte)(registers.E>>1);  registers.flagZ=(registers.E==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_3C() { registers.flagC=((registers.H&0x01)!=0);  registers.H=(byte)(registers.H>>1);  registers.flagZ=(registers.H==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_3D() { registers.flagC=((registers.L&0x01)!=0);  registers.L=(byte)(registers.L>>1);  registers.flagZ=(registers.L==0);  registers.flagH=false;  registers.flagN=false; }
		void CB_3E() { registers.flagC=((mmu.Read(registers.HL)&0x01)!=0);  mmu.Write(registers.HL, (byte)(mmu.Read(registers.HL)>>1));  registers.flagZ=(mmu.Read(registers.HL)==0);  registers.flagH=false;  registers.flagN=false; }

		//bit-n-r
		//bit A
		void CB_47() { registers.flagZ=((registers.A&0x01)==0); registers.flagN=false; registers.flagH=true; }
		void CB_4F() { registers.flagZ=((registers.A&0x02)==0); registers.flagN=false; registers.flagH=true; }
		void CB_57() { registers.flagZ=((registers.A&0x04)==0); registers.flagN=false; registers.flagH=true; }
		void CB_5F() { registers.flagZ=((registers.A&0x08)==0); registers.flagN=false; registers.flagH=true; }
		void CB_67() { registers.flagZ=((registers.A&0x10)==0); registers.flagN=false; registers.flagH=true; }
		void CB_6F() { registers.flagZ=((registers.A&0x20)==0); registers.flagN=false; registers.flagH=true; }
		void CB_77() { registers.flagZ=((registers.A&0x40)==0); registers.flagN=false; registers.flagH=true; }
		void CB_7F() { registers.flagZ=((registers.A&0x80)==0); registers.flagN=false; registers.flagH=true; }

		//bit B
		void CB_40() { registers.flagZ=((registers.B&0x01)==0); registers.flagN=false; registers.flagH=true; }
		void CB_48() { registers.flagZ=((registers.B&0x02)==0); registers.flagN=false; registers.flagH=true; }
		void CB_50() { registers.flagZ=((registers.B&0x04)==0); registers.flagN=false; registers.flagH=true; }
		void CB_58() { registers.flagZ=((registers.B&0x08)==0); registers.flagN=false; registers.flagH=true; }
		void CB_60() { registers.flagZ=((registers.B&0x10)==0); registers.flagN=false; registers.flagH=true; }
		void CB_68() { registers.flagZ=((registers.B&0x20)==0); registers.flagN=false; registers.flagH=true; }
		void CB_70() { registers.flagZ=((registers.B&0x40)==0); registers.flagN=false; registers.flagH=true; }
		void CB_78() { registers.flagZ=((registers.B&0x80)==0); registers.flagN=false; registers.flagH=true; }

		//bit C
		void CB_41() { registers.flagZ=((registers.C&0x01)==0); registers.flagN=false; registers.flagH=true; }
		void CB_49() { registers.flagZ=((registers.C&0x02)==0); registers.flagN=false; registers.flagH=true; }
		void CB_51() { registers.flagZ=((registers.C&0x04)==0); registers.flagN=false; registers.flagH=true; }
		void CB_59() { registers.flagZ=((registers.C&0x08)==0); registers.flagN=false; registers.flagH=true; }
		void CB_61() { registers.flagZ=((registers.C&0x10)==0); registers.flagN=false; registers.flagH=true; }
		void CB_69() { registers.flagZ=((registers.C&0x20)==0); registers.flagN=false; registers.flagH=true; }
		void CB_71() { registers.flagZ=((registers.C&0x40)==0); registers.flagN=false; registers.flagH=true; }
		void CB_79() { registers.flagZ=((registers.C&0x80)==0); registers.flagN=false; registers.flagH=true; }

		//bit D
		void CB_42() { registers.flagZ=((registers.D&0x01)==0); registers.flagN=false; registers.flagH=true; }
		void CB_4A() { registers.flagZ=((registers.D&0x02)==0); registers.flagN=false; registers.flagH=true; }
		void CB_52() { registers.flagZ=((registers.D&0x04)==0); registers.flagN=false; registers.flagH=true; }
		void CB_5A() { registers.flagZ=((registers.D&0x08)==0); registers.flagN=false; registers.flagH=true; }
		void CB_62() { registers.flagZ=((registers.D&0x10)==0); registers.flagN=false; registers.flagH=true; }
		void CB_6A() { registers.flagZ=((registers.D&0x20)==0); registers.flagN=false; registers.flagH=true; }
		void CB_72() { registers.flagZ=((registers.D&0x40)==0); registers.flagN=false; registers.flagH=true; }
		void CB_7A() { registers.flagZ=((registers.D&0x80)==0); registers.flagN=false; registers.flagH=true; }

		//bit E
		void CB_43() { registers.flagZ=((registers.E&0x01)==0); registers.flagN=false; registers.flagH=true; }
		void CB_4B() { registers.flagZ=((registers.E&0x02)==0); registers.flagN=false; registers.flagH=true; }
		void CB_53() { registers.flagZ=((registers.E&0x04)==0); registers.flagN=false; registers.flagH=true; }
		void CB_5B() { registers.flagZ=((registers.E&0x08)==0); registers.flagN=false; registers.flagH=true; }
		void CB_63() { registers.flagZ=((registers.E&0x10)==0); registers.flagN=false; registers.flagH=true; }
		void CB_6B() { registers.flagZ=((registers.E&0x20)==0); registers.flagN=false; registers.flagH=true; }
		void CB_73() { registers.flagZ=((registers.E&0x40)==0); registers.flagN=false; registers.flagH=true; }
		void CB_7B() { registers.flagZ=((registers.E&0x80)==0); registers.flagN=false; registers.flagH=true; }

		//bit H
		void CB_44() { registers.flagZ=((registers.H&0x01)==0); registers.flagN=false; registers.flagH=true; }
		void CB_4C() { registers.flagZ=((registers.H&0x02)==0); registers.flagN=false; registers.flagH=true; }
		void CB_54() { registers.flagZ=((registers.H&0x04)==0); registers.flagN=false; registers.flagH=true; }
		void CB_5C() { registers.flagZ=((registers.H&0x08)==0); registers.flagN=false; registers.flagH=true; }
		void CB_64() { registers.flagZ=((registers.H&0x10)==0); registers.flagN=false; registers.flagH=true; }
		void CB_6C() { registers.flagZ=((registers.H&0x20)==0); registers.flagN=false; registers.flagH=true; }
		void CB_74() { registers.flagZ=((registers.H&0x40)==0); registers.flagN=false; registers.flagH=true; }
		void CB_7C() { registers.flagZ=((registers.H&0x80)==0); registers.flagN=false; registers.flagH=true; }

		//bit L
		void CB_45() { registers.flagZ=((registers.L&0x01)==0); registers.flagN=false; registers.flagH=true; }
		void CB_4D() { registers.flagZ=((registers.L&0x02)==0); registers.flagN=false; registers.flagH=true; }
		void CB_55() { registers.flagZ=((registers.L&0x04)==0); registers.flagN=false; registers.flagH=true; }
		void CB_5D() { registers.flagZ=((registers.L&0x08)==0); registers.flagN=false; registers.flagH=true; }
		void CB_65() { registers.flagZ=((registers.L&0x10)==0); registers.flagN=false; registers.flagH=true; }
		void CB_6D() { registers.flagZ=((registers.L&0x20)==0); registers.flagN=false; registers.flagH=true; }
		void CB_75() { registers.flagZ=((registers.L&0x40)==0); registers.flagN=false; registers.flagH=true; }
		void CB_7D() { registers.flagZ=((registers.L&0x80)==0); registers.flagN=false; registers.flagH=true; }

		//bit (HL)
		void CB_46() { registers.flagZ=((mmu.Read(registers.HL)&0x01)==0); registers.flagN=false; registers.flagH=true; }
		void CB_4E() { registers.flagZ=((mmu.Read(registers.HL)&0x02)==0); registers.flagN=false; registers.flagH=true; }
		void CB_56() { registers.flagZ=((mmu.Read(registers.HL)&0x04)==0); registers.flagN=false; registers.flagH=true; }
		void CB_5E() { registers.flagZ=((mmu.Read(registers.HL)&0x08)==0); registers.flagN=false; registers.flagH=true; }
		void CB_66() { registers.flagZ=((mmu.Read(registers.HL)&0x10)==0); registers.flagN=false; registers.flagH=true; }
		void CB_6E() { registers.flagZ=((mmu.Read(registers.HL)&0x20)==0); registers.flagN=false; registers.flagH=true; }
		void CB_76() { registers.flagZ=((mmu.Read(registers.HL)&0x40)==0); registers.flagN=false; registers.flagH=true; }
		void CB_7E() { registers.flagZ=((mmu.Read(registers.HL)&0x80)==0); registers.flagN=false; registers.flagH=true; }

		//set A
		void CB_C7() { registers.A=(byte)(registers.A|0x01); }
		void CB_CF() { registers.A=(byte)(registers.A|0x02); }
		void CB_D7() { registers.A=(byte)(registers.A|0x04); }
		void CB_DF() { registers.A=(byte)(registers.A|0x08); }
		void CB_E7() { registers.A=(byte)(registers.A|0x10); }
		void CB_EF() { registers.A=(byte)(registers.A|0x20); }
		void CB_F7() { registers.A=(byte)(registers.A|0x40); }
		void CB_FF() { registers.A=(byte)(registers.A|0x80); }

		//set B
		void CB_C0() { registers.B=(byte)(registers.B|0x01); }
		void CB_C8() { registers.B=(byte)(registers.B|0x02); }
		void CB_D0() { registers.B=(byte)(registers.B|0x04); }
		void CB_D8() { registers.B=(byte)(registers.B|0x08); }
		void CB_E0() { registers.B=(byte)(registers.B|0x10); }
		void CB_E8() { registers.B=(byte)(registers.B|0x20); }
		void CB_F0() { registers.B=(byte)(registers.B|0x40); }
		void CB_F8() { registers.B=(byte)(registers.B|0x80); }

		//set C
		void CB_C1() { registers.C=(byte)(registers.C|0x01); }
		void CB_C9() { registers.C=(byte)(registers.C|0x02); }
		void CB_D1() { registers.C=(byte)(registers.C|0x04); }
		void CB_D9() { registers.C=(byte)(registers.C|0x08); }
		void CB_E1() { registers.C=(byte)(registers.C|0x10); }
		void CB_E9() { registers.C=(byte)(registers.C|0x20); }
		void CB_F1() { registers.C=(byte)(registers.C|0x40); }
		void CB_F9() { registers.C=(byte)(registers.C|0x80); }

		//set D
		void CB_C2() { registers.D=(byte)(registers.D|0x01); }
		void CB_CA() { registers.D=(byte)(registers.D|0x02); }
		void CB_D2() { registers.D=(byte)(registers.D|0x04); }
		void CB_DA() { registers.D=(byte)(registers.D|0x08); }
		void CB_E2() { registers.D=(byte)(registers.D|0x10); }
		void CB_EA() { registers.D=(byte)(registers.D|0x20); }
		void CB_F2() { registers.D=(byte)(registers.D|0x40); }
		void CB_FA() { registers.D=(byte)(registers.D|0x80); }

		//set E
		void CB_C3() { registers.E=(byte)(registers.E|0x01); }
		void CB_CB() { registers.E=(byte)(registers.E|0x02); }
		void CB_D3() { registers.E=(byte)(registers.E|0x04); }
		void CB_DB() { registers.E=(byte)(registers.E|0x08); }
		void CB_E3() { registers.E=(byte)(registers.E|0x10); }
		void CB_EB() { registers.E=(byte)(registers.E|0x20); }
		void CB_F3() { registers.E=(byte)(registers.E|0x40); }
		void CB_FB() { registers.E=(byte)(registers.E|0x80); }

		//set H
		void CB_C4() { registers.H=(byte)(registers.H|0x01); }
		void CB_CC() { registers.H=(byte)(registers.H|0x02); }
		void CB_D4() { registers.H=(byte)(registers.H|0x04); }
		void CB_DC() { registers.H=(byte)(registers.H|0x08); }
		void CB_E4() { registers.H=(byte)(registers.H|0x10); }
		void CB_EC() { registers.H=(byte)(registers.H|0x20); }
		void CB_F4() { registers.H=(byte)(registers.H|0x40); }
		void CB_FC() { registers.H=(byte)(registers.H|0x80); }

		//set L
		void CB_C5() { registers.L=(byte)(registers.L|0x01); }
		void CB_CD() { registers.L=(byte)(registers.L|0x02); }
		void CB_D5() { registers.L=(byte)(registers.L|0x04); }
		void CB_DD() { registers.L=(byte)(registers.L|0x08); }
		void CB_E5() { registers.L=(byte)(registers.L|0x10); }
		void CB_ED() { registers.L=(byte)(registers.L|0x20); }
		void CB_F5() { registers.L=(byte)(registers.L|0x40); }
		void CB_FD() { registers.L=(byte)(registers.L|0x80); }

		//set (HL)
		void CB_C6() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)|0x01)); }
		void CB_CE() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)|0x02)); }
		void CB_D6() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)|0x04)); }
		void CB_DE() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)|0x08)); }
		void CB_E6() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)|0x10)); }
		void CB_EE() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)|0x20)); }
		void CB_F6() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)|0x40)); }
		void CB_FE() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)|0x80)); }

		//res A
		void CB_87() { registers.A=(byte)(registers.A&0xFE); }
		void CB_8F() { registers.A=(byte)(registers.A&0xFD); }
		void CB_97() { registers.A=(byte)(registers.A&0xFB); }
		void CB_9F() { registers.A=(byte)(registers.A&0xF7); }
		void CB_A7() { registers.A=(byte)(registers.A&0xEF); }
		void CB_AF() { registers.A=(byte)(registers.A&0xDF); }
		void CB_B7() { registers.A=(byte)(registers.A&0xBF); }
		void CB_BF() { registers.A=(byte)(registers.A&0x7F); }

		//res B
		void CB_80() { registers.B=(byte)(registers.B&0xFE); }
		void CB_88() { registers.B=(byte)(registers.B&0xFD); }
		void CB_90() { registers.B=(byte)(registers.B&0xFB); }
		void CB_98() { registers.B=(byte)(registers.B&0xF7); }
		void CB_A0() { registers.B=(byte)(registers.B&0xEF); }
		void CB_A8() { registers.B=(byte)(registers.B&0xDF); }
		void CB_B0() { registers.B=(byte)(registers.B&0xBF); }
		void CB_B8() { registers.B=(byte)(registers.B&0x7F); }

		//res C
		void CB_81() { registers.C=(byte)(registers.C&0xFE); }
		void CB_89() { registers.C=(byte)(registers.C&0xFD); }
		void CB_91() { registers.C=(byte)(registers.C&0xFB); }
		void CB_99() { registers.C=(byte)(registers.C&0xF7); }
		void CB_A1() { registers.C=(byte)(registers.C&0xEF); }
		void CB_A9() { registers.C=(byte)(registers.C&0xDF); }
		void CB_B1() { registers.C=(byte)(registers.C&0xBF); }
		void CB_B9() { registers.C=(byte)(registers.C&0x7F); }

		//res D
		void CB_82() { registers.D=(byte)(registers.D&0xFE); }
		void CB_8A() { registers.D=(byte)(registers.D&0xFD); }
		void CB_92() { registers.D=(byte)(registers.D&0xFB); }
		void CB_9A() { registers.D=(byte)(registers.D&0xF7); }
		void CB_A2() { registers.D=(byte)(registers.D&0xEF); }
		void CB_AA() { registers.D=(byte)(registers.D&0xDF); }
		void CB_B2() { registers.D=(byte)(registers.D&0xBF); }
		void CB_BA() { registers.D=(byte)(registers.D&0x7F); }

		//res E
		void CB_83() { registers.E=(byte)(registers.E&0xFE); }
		void CB_8B() { registers.E=(byte)(registers.E&0xFD); }
		void CB_93() { registers.E=(byte)(registers.E&0xFB); }
		void CB_9B() { registers.E=(byte)(registers.E&0xF7); }
		void CB_A3() { registers.E=(byte)(registers.E&0xEF); }
		void CB_AB() { registers.E=(byte)(registers.E&0xDF); }
		void CB_B3() { registers.E=(byte)(registers.E&0xBF); }
		void CB_BB() { registers.E=(byte)(registers.E&0x7F); }

		//res H
		void CB_84() { registers.H=(byte)(registers.H&0xFE); }
		void CB_8C() { registers.H=(byte)(registers.H&0xFD); }
		void CB_94() { registers.H=(byte)(registers.H&0xFB); }
		void CB_9C() { registers.H=(byte)(registers.H&0xF7); }
		void CB_A4() { registers.H=(byte)(registers.H&0xEF); }
		void CB_AC() { registers.H=(byte)(registers.H&0xDF); }
		void CB_B4() { registers.H=(byte)(registers.H&0xBF); }
		void CB_BC() { registers.H=(byte)(registers.H&0x7F); }

		//res L
		void CB_85() { registers.L=(byte)(registers.L&0xFE); }
		void CB_8D() { registers.L=(byte)(registers.L&0xFD); }
		void CB_95() { registers.L=(byte)(registers.L&0xFB); }
		void CB_9D() { registers.L=(byte)(registers.L&0xF7); }
		void CB_A5() { registers.L=(byte)(registers.L&0xEF); }
		void CB_AD() { registers.L=(byte)(registers.L&0xDF); }
		void CB_B5() { registers.L=(byte)(registers.L&0xBF); }
		void CB_BD() { registers.L=(byte)(registers.L&0x7F); }

		//res (HL)
		void CB_86() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)&0xFE)); }
		void CB_8E() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)&0xFD)); }
		void CB_96() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)&0xFB)); }
		void CB_9E() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)&0xF7)); }
		void CB_A6() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)&0xEF)); }
		void CB_AE() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)&0xDF)); }
		void CB_B6() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)&0xBF)); }
		void CB_BE() { mmu.Write(registers.HL,(byte)(mmu.Read(registers.HL)&0x7F)); }


		#endregion





		#region Helpers & others

		int DecodeSigned(byte b)
		{
			int result = (int)b;
			if (b > 127) {
				result -= 0x100;
			}
			return result;
		}

		void OP_XX() {
			Debug.LogError(string.Format("Invalid operation received: {0}", registers.PC));
			stop = true;
		}

		#endregion
	}


}
