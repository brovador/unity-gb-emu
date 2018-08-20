using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace brovador.GBEmulator {

	public class Joypad {

		public enum Button {
			Up,
			Down,
			Left,
			Right,
			A,
			B,
			Select,
			Start
		}

		Dictionary<Button, bool> states;
		MMU mmu;

		public Joypad(MMU mmu)
		{
			this.mmu = mmu;
			states = new Dictionary<Button, bool>();
			foreach (Button btn in System.Enum.GetValues(typeof(Button))) {
				states[btn] = false;
			}
		}


		public void SetKey(Button button, bool pressed)
		{
			states[button] = pressed;
			byte directions = 0x00;
			byte buttons = 0x00;

			buttons |= (byte)(states[Button.A] ? 0x01 : 0x00);
			buttons |= (byte)(states[Button.B] ? 0x02 : 0x00);
			buttons |= (byte)(states[Button.Select] ? 0x04 : 0x00);
			buttons |= (byte)(states[Button.Start] ? 0x08 : 0x00);
			buttons = (byte)~buttons;

			directions |= (byte)(states[Button.Right] ? 0x01 : 0x00);
			directions |= (byte)(states[Button.Left] ? 0x02 : 0x00);
			directions |= (byte)(states[Button.Up] ? 0x04 : 0x00);
			directions |= (byte)(states[Button.Down] ? 0x08 : 0x00);
			directions = (byte)~directions;

			//0 is pressed, so we invert the bits
			//before writing in memory
			mmu.WriteJoypadInfo(
				(byte)((buttons) & 0x0F), 
				(byte)((directions) & 0xF)
			);

			if (directions != 0x0F || buttons != 0x0F) {
				mmu.SetInterrupt(MMU.InterruptType.HighToLowP10P13);
			}
		}
	}
}
