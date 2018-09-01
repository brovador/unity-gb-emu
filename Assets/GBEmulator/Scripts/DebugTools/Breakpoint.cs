using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using brovador.GBEmulator;

namespace brovador.GBEmulator.Debugger {

	[System.Serializable]
	public class Breakpoint {

		public enum Condition {
			None,
			RegisterAF,
			RegisterBC,
			RegisterDE,
			RegisterHL,
			RegisterPC,
			RegisterSP,
			AddressValue
		}

		public bool active = true;
		public string address;
		public Condition condition;
		public string conditionValue;


		public bool IsActivated(ushort addr, Emulator emu) {
			var result = this.active;
			if (!result)
				return false;

			ushort addressValue = System.Convert.ToUInt16(address, 16);
			result = result && addr == addressValue;
			if (result && condition != Condition.None && conditionValue != string.Empty) {
					ushort value = System.Convert.ToUInt16(conditionValue, 16);
				ushort value2 = 0;
				switch (condition) {
				case Condition.RegisterAF:
					value2 = emu.cpu.registers.AF;
					break;
				case Condition.RegisterBC:
					value2 = emu.cpu.registers.BC;
					break;
				case Condition.RegisterDE:
					value2 = emu.cpu.registers.DE;
					break;
				case Condition.RegisterHL:
					value2 = emu.cpu.registers.HL;
					break;
				case Condition.RegisterPC:
					value2 = emu.cpu.registers.PC;
					break;
				case Condition.RegisterSP:
					value2 = emu.cpu.registers.SP;
					break;
				case Condition.AddressValue:
					value2 = emu.mmu.Read(addressValue);
					break;
				}
				result = result && value == value2;
			}
			return result;
		}
	}
}
