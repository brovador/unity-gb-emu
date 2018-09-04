using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace brovador.GBEmulator {
	public class MMU {

		public event System.Action<MMU, ushort> OnMemoryWritten;

		public enum CartdrigeType {
			NoMBC,
			MBC1,
			MBCRAM,
			MBCBatteryRAM
		}

		public enum InterruptType {
			VBlank,
			LCDCStatus,
			TimerOverflow,
			SerialTransferCompletion,
			HighToLowP10P13
		}

		byte[] bios = {};
		byte[] memory;
		byte[] rom;
		CartdrigeType cartType;

		byte joypadButtons = 0x0F;
		byte joypadDirections = 0x0F;
		uint romOffset = 0x00;

		public bool inBios = true;

		public MMU()
		{
			memory = new byte[0x10000];
			romOffset = 0x00;
			joypadButtons = 0x0F;
			joypadDirections = 0x0F;

			//IO starts in general with FF (bgb)
			for (int i = 0xFF00; i < 0xFF4C; i++) {
				Write((ushort)i, (byte)0xFF);
			}

		}


		public void LoadRom(byte[] romContent)
		{
			rom = new byte[romContent.Length];
			for (int i = 0; i < romContent.Length; i++) {
				rom[i] = romContent[i];
			}
			cartType = (CartdrigeType)(Read(0x0147) & 0x03);
		}


		public byte Read(ushort addr) {
			var result = (byte)0;

			//Exit bios when address is 0x0100
			inBios = (inBios && addr != 0x0100);

			if (inBios && addr < 0x0100) {
				result = bios[addr];
			} 
			//ROM 
			else if (addr < 0x4000) {
				result = rom[addr];
			}
			//Switchable ROM
			else if (addr < 0x8000) {
				result = rom[romOffset + addr];
			}
			//Joypad read
			else if (addr == 0xFF00) {

				var tmp = memory[0xFF00];
				var h = tmp & 0xF0;
				var l = tmp & 0x0F;

				//Select direction
				if ((h & 0x20) != 0x00) {
					l = joypadDirections;
				} else if ((h & 0x10) != 0x00) {
					l = joypadButtons;
				}

				result = (byte)(h + l);

			} else {
				result = memory[addr];
			}

			return result;
		}


		public ushort ReadW(ushort addr) {
			ushort l = (ushort)(Read(addr));
			ushort h = (ushort)(Read((ushort)(addr + 1)));
			return (ushort)((h << 8 &0xFF00) + l);
		}


		public void Write(ushort addr, byte data, bool allowReadOnlyWrite = false) {

			bool allowWrite = true;

			//ROM area
			if (addr < 0x8000) {
				allowWrite = allowReadOnlyWrite;

				#warning MMU: implement remaining memory banking modes
				//Memory banking: external ram
				if (addr < 0x2000) {
				} 
				//Memory banking: ROM bank
				else if (addr < 0x4000) {
					var bank = (data & 0x1F);
					if (bank == 0) {
						bank = 1;
					}
					romOffset = (uint)(0x4000 * (bank - 1));
				}
				//Memory banking: ROM bank + RAM bank
				else if (addr < 0x6000) {
				} 
				//Mode: 0 ROM mode, 1 RAM mode
				else {
				}

			}
			//Joypad
			else if (addr == 0xFF00) {
				data = (byte)((memory[addr] & 0x0F) + (data & 0xF0));
			}
			//Divider, reset if a write is done here
			else if (addr == 0xFF04) {
				data = allowReadOnlyWrite ? data : (byte)0x00;
			}
			//LY, reset if a write is done here
			else if (addr == 0xFF44) {
				data = allowReadOnlyWrite ? data : (byte)0x00;
			}
			//Echoing RAM
			else if (addr >= 0xE000 && addr < 0xFE00) {
				memory[0xC000 + (addr - 0xE000)] = data;
			} 
			//Echoing RAM
			else if (addr >= 0xC000 && addr < 0xDE00) {
				memory[0xE000 + (addr - 0xC000)] = data;
			}

			if (allowWrite) {
				memory[addr] = data;
				if (OnMemoryWritten != null) {
					OnMemoryWritten(this, addr);
				}
			}
		}


		public void WriteW(ushort addr, ushort data) {
			Write(addr, (byte)(data & 0x00FF));
			Write((ushort)(addr + 1), (byte)((data & 0xFF00) >> 8));
		}


		public void WriteJoypadInfo(byte buttons, byte directions)
		{
			joypadButtons = buttons;
			joypadDirections = directions;
		}


		#region Interrupts

		byte IF {
			get { return Read(0xFF0F); }
			set { Write(0xFF0F, value); }
		}

		byte IE {
			get { return Read(0xFFFF); }
			set { Write(0xFFFF, value); }
		}

		public bool HasInterrupts() {
			return (IE & IF) != 0;
		}


		public bool CheckInterruptEnabled(InterruptType interruptType)
		{
			var result = false;
			switch (interruptType) {
			case InterruptType.VBlank:
				result = (IE & 0x01) != 0x00;
				break;
			case InterruptType.LCDCStatus:
				result = (IE & 0x02) != 0x00;
				break;
			case InterruptType.TimerOverflow:
				result = (IE & 0x04) != 0x00;
				break;
			case InterruptType.SerialTransferCompletion:
				result = (IE & 0x08) != 0x00;
				break;
			case InterruptType.HighToLowP10P13:
				result = (IE & 0x10) != 0x00;
				break;
			}
			return result;
		}


		public void EnableInterrupt(InterruptType interruptType)
		{
			switch (interruptType) {
			case InterruptType.VBlank:
				IE |= 0x01;
				break;
			case InterruptType.LCDCStatus:
				IE |= 0x02;
				break;
			case InterruptType.TimerOverflow:
				IE |= 0x04;
				break;
			case InterruptType.SerialTransferCompletion:
				IE |= 0x08;
				break;
			case InterruptType.HighToLowP10P13:
				IE |= 0x10;
				break;
			}
		}


		public void DisableInterrupt(InterruptType interruptType)
		{
			switch (interruptType) {
			case InterruptType.VBlank:
				IE = (byte)(IE & (~0x01));
				break;
			case InterruptType.LCDCStatus:
				IE = (byte)(IE & (~0x02));
				break;
			case InterruptType.TimerOverflow:
				IE = (byte)(IE & (~0x04));
				break;
			case InterruptType.SerialTransferCompletion:
				IE = (byte)(IE & (~0x08));
				break;
			case InterruptType.HighToLowP10P13:
				IE = (byte)(IE & (~0x10));
				break;
			}
		}



		public bool CheckInterrupt(InterruptType interruptType)
		{
			var result = CheckInterruptEnabled(interruptType);
			if (result) {
				switch (interruptType) {
				case InterruptType.VBlank:
					result = (IF & 0x01) != 0x00;
					break;
				case InterruptType.LCDCStatus:
					result = (IF & 0x02) != 0x00;
					break;
				case InterruptType.TimerOverflow:
					result = (IF & 0x04) != 0x00;
					break;
				case InterruptType.SerialTransferCompletion:
					result = (IF & 0x08) != 0x00;
					break;
				case InterruptType.HighToLowP10P13:
					result = (IF & 0x10) != 0x00;
					break;
				}
			}
			return result;
		}


		public void SetInterrupt(InterruptType interruptType)
		{
			switch (interruptType) {
			case InterruptType.VBlank:
				IF |= 0x01;
				break;
			case InterruptType.LCDCStatus:
				IF |= 0x02;
				break;
			case InterruptType.TimerOverflow:
				IF |= 0x04;
				break;
			case InterruptType.SerialTransferCompletion:
				IF |= 0x08;
				break;
			case InterruptType.HighToLowP10P13:
				IF |= 0x10;
				break;
			}
		}


		public void ClearInterrupt(InterruptType interruptType)
		{
			switch (interruptType) {
			case InterruptType.VBlank:
				IF = (byte)(IF & (~0x01));
				break;
			case InterruptType.LCDCStatus:
				IF = (byte)(IF & (~0x02));
				break;
			case InterruptType.TimerOverflow:
				IF = (byte)(IF & (~0x04));
				break;
			case InterruptType.SerialTransferCompletion:
				IF = (byte)(IF & (~0x08));
				break;
			case InterruptType.HighToLowP10P13:
				IF = (byte)(IF & (~0x10));
				break;
			}
		}


		#endregion
	}
}