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

			public UInt16 AF { 
				get { return (UInt16)(((UInt16)A) << 8 + F); } 
				set { A = (byte)((value & 0xFF00) >> 8); F = (byte)(value & 0x00FF); }
			}
			public UInt16 BC { 
				get { return (UInt16)(((UInt16)B) << 8 + C); } 
				set { B = (byte)((value & 0xFF00) >> 8); C = (byte)(value & 0x00FF); }
			}
			public UInt16 DE { 
				get { return (UInt16)(((UInt16)D) << 8 + E); } 
				set { D = (byte)((value & 0xFF00) >> 8); E = (byte)(value & 0x00FF); }
			}
			public UInt16 HL { 
				get { return (UInt16)(((UInt16)H) << 8 + L); } 
				set { H = (byte)((value & 0xFF00) >> 8); L = (byte)(value & 0x00FF); }
			}

			public bool flagZ {
				set { this.F = (byte)(value ? this.F & 0x80 : this.F & 0x80); }
				get { return ((this.F & 0x80) != 0x00); }
			}
			public bool flagN {
				set { this.F = (byte)(value ? this.F & 0x40 : this.F & 0x40); }
				get { return ((this.F & 0x40) != 0x00); }
			}
			public bool flagH {
				set { this.F = (byte)(value ? this.F & 0x20 : this.F & 0x20); }
				get { return ((this.F & 0x20) != 0x00); }
			}
			public bool flagC {
				set { this.F = (byte)(value ? this.F & 0x10 : this.F & 0x10); }
				get { return ((this.F & 0x10) != 0x00); }
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
//		public System.Action[] operations = {
//			OP_00,OP_01,OP_02,OP_03,OP_04,OP_05,OP_06,OP_07,OP_08,OP_09,OP_0A,OP_0B,OP_0C,OP_0D,OP_0E,OP_0F,
//			OP_10,OP_11,OP_12,OP_13,OP_14,OP_15,OP_16,OP_17,OP_18,OP_19,OP_1A,OP_1B,OP_1C,OP_1D,OP_1E,OP_1F,
//			OP_20,OP_21,OP_22,OP_23,OP_24,OP_25,OP_26,OP_27,OP_28,OP_29,OP_2A,OP_2B,OP_2C,OP_2D,OP_2E,OP_2F,
//			OP_30,OP_31,OP_32,OP_33,OP_34,OP_35,OP_36,OP_37,OP_38,OP_39,OP_3A,OP_3B,OP_3C,OP_3D,OP_3E,OP_3F,
//			OP_40,OP_41,OP_42,OP_43,OP_44,OP_45,OP_46,OP_47,OP_48,OP_49,OP_4A,OP_4B,OP_4C,OP_4D,OP_4E,OP_4F,
//			OP_50,OP_51,OP_52,OP_53,OP_54,OP_55,OP_56,OP_57,OP_58,OP_59,OP_5A,OP_5B,OP_5C,OP_5D,OP_5E,OP_5F,
//			OP_60,OP_61,OP_62,OP_63,OP_64,OP_65,OP_66,OP_67,OP_68,OP_69,OP_6A,OP_6B,OP_6C,OP_6D,OP_6E,OP_6F,
//			OP_70,OP_71,OP_72,OP_73,OP_74,OP_75,OP_76,OP_77,OP_78,OP_79,OP_7A,OP_7B,OP_7C,OP_7D,OP_7E,OP_7F,
//			OP_80,OP_81,OP_82,OP_83,OP_84,OP_85,OP_86,OP_87,OP_88,OP_89,OP_8A,OP_8B,OP_8C,OP_8D,OP_8E,OP_8F,
//			OP_90,OP_91,OP_92,OP_93,OP_94,OP_95,OP_96,OP_97,OP_98,OP_99,OP_9A,OP_9B,OP_9C,OP_9D,OP_9E,OP_9F,
//			OP_A0,OP_A1,OP_A2,OP_A3,OP_A4,OP_A5,OP_A6,OP_A7,OP_A8,OP_A9,OP_AA,OP_AB,OP_AC,OP_AD,OP_AE,OP_AF,
//			OP_B0,OP_B1,OP_B2,OP_B3,OP_B4,OP_B5,OP_B6,OP_B7,OP_B8,OP_B9,OP_BA,OP_BB,OP_BC,OP_BD,OP_BE,OP_BF,
//			OP_C0,OP_C1,OP_C2,OP_C3,OP_C4,OP_C5,OP_C6,OP_C7,OP_C8,OP_C9,OP_CA,OP_CB,OP_CC,OP_CD,OP_CE,OP_CF,
//			OP_D0,OP_D1,OP_D2,OP_D3,OP_D4,OP_D5,OP_D6,OP_D7,OP_D8,OP_D9,OP_DA,OP_DB,OP_DC,OP_DD,OP_DE,OP_DF,
//			OP_E0,OP_E1,OP_E2,OP_E3,OP_E4,OP_E5,OP_E6,OP_E7,OP_E8,OP_E9,OP_EA,OP_EB,OP_EC,OP_ED,OP_EE,OP_EF,
//			OP_F0,OP_F1,OP_F2,OP_F3,OP_F4,OP_F5,OP_F6,OP_F7,OP_F8,OP_F9,OP_FA,OP_FB,OP_FC,OP_FD,OP_FE,OP_FF
//		};

		public CPU(MMU mmu) {
			this.mmu = mmu;

			timers.t = 0;
			timers.m = 0;

			registers.AF = 0x01B0; //0x01=GB/SGB, 0xFF=GBP, 0x11=GBC
			registers.BC = 0x0013;
			registers.DE = 0x00D8;
			registers.HL = 0x014D;
			registers.SP = 0xFFFE;
			registers.PC = 0x0100;

			operations = new System.Action[256];
		}

		public void Step()
		{
			var op = mmu.Read(registers.PC++);
			operations[op]();
			timers.t += tTimes[op];
			timers.m += mTimes[op];
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
		void OP_FA() { registers.A=mmu.Read(mmu.ReadW(registers.PC+=2)); } //LD A (nn)
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
		void OP_EA() { mmu.WriteW(registers.PC+=2, registers.A); } //LD (nn) A

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
		void OP_01() { registers.BC=mmu.Read(registers.PC+=2); } //LD BC,nn
		void OP_11() { registers.DE=mmu.Read(registers.PC+=2); } //LD DE,nn
		void OP_21() { registers.HL=mmu.Read(registers.PC+=2); } //LD HL,nn
		void OP_31() { registers.SP=mmu.ReadW(registers.PC+=2); } //LD SP,nn

		//ld-sp-hl
		void OP_F9() { registers.SP=registers.HL; } //LD SP,HL

		//ldhl-sp-n
		//TODO: set flags carry and half-carry
		void OP_F8() { registers.HL=(UInt16)(registers.SP+mmu.Read(registers.PC++)); registers.flagZ=false; registers.flagN=false; } //LDHL SP,n 

		//ld-nn-sp
		void OP_08() { mmu.WriteW(mmu.ReadW(registers.PC+=2), registers.SP); } //LD (nn),SP

		//push-nn
		void OP_F5() { mmu.WriteW(--registers.SP,registers.AF); registers.SP--; }// PUSH AF
		void OP_C5() { mmu.WriteW(--registers.SP,registers.BC); registers.SP--; }// PUSH BC
		void OP_D5() { mmu.WriteW(--registers.SP,registers.DE); registers.SP--; }// PUSH DE
		void OP_E5() { mmu.WriteW(--registers.SP,registers.HL); registers.SP--; }// PUSH HL

		//pop-nn
		void OP_F1() { registers.AF=mmu.ReadW(registers.SP+=2); }  //POP AF
		void OP_C1() { registers.BC=mmu.ReadW(registers.SP+=2); }  //POP BC
		void OP_D1() { registers.DE=mmu.ReadW(registers.SP+=2); }  //POP DE
		void OP_E1() { registers.HL=mmu.ReadW(registers.SP+=2); }  //POP HL

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
		void OP_FE() { registers.flagZ=(registers.A==mmu.Read(registers.PC++)); registers.flagN=true; registers.flagH=((registers.A&0x0F)<(mmu.Read(registers.PC++)&0x0F)); registers.flagC=(registers.A<mmu.Read(registers.PC++)); } //CP #

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
		#warning Check flags
		void OP_E8() { registers.SP+=mmu.Read(registers.PC++); registers.flagZ=false; registers.flagN=false; }

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
	}

}
