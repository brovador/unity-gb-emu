using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace brovador.GBEmulator {

	public class DebugTools : MonoBehaviour{

		string[] opCodes = {
			"NOP", "LD BC,d16", "LD (BC),A", "INC BC", "INC B", "DEC B", "LD B,d8", "RLCA", "LD (a16),SP", "ADD HL,BC", "LD A,(BC)", "DEC BC", "INC C", "DEC C", "LD C,d8", "RRCA",
			"STOP 0", "LD DE,d16", "LD (DE),A", "INC DE", "INC D", "DEC D", "LD D,d8", "RLA", "JR r8", "ADD HL,DE", "LD A,(DE)", "DEC DE", "INC E", "DEC E", "LD E,d8", "RRA",
			"JR NZ,r8", "LD HL,d16", "LD (HL+),A", "INC HL", "INC H", "DEC H", "LD H,d8", "DAA", "JR Z,r8", "ADD HL,HL", "LD A,(HL+)", "DEC HL", "INC L", "DEC L", "LD L,d8", "CPL",
			"JR NC,r8", "LD SP,d16", "LD (HL-),A", "INC SP", "INC (HL)", "DEC (HL)", "LD (HL),d8", "SCF", "JR C,r8", "ADD HL,SP", "LD A,(HL-)", "DEC SP", "INC A", "DEC A", "LD A,d8", "CCF",
			"LD B,B", "LD B,C", "LD B,D", "LD B,E", "LD B,H", "LD B,L", "LD B,(HL)", "LD B,A", "LD C,B", "LD C,C", "LD C,D", "LD C,E", "LD C,H", "LD C,L", "LD C,(HL)", "LD C,A",
			"LD D,B", "LD D,C", "LD D,D", "LD D,E", "LD D,H", "LD D,L", "LD D,(HL)", "LD D,A", "LD E,B", "LD E,C", "LD E,D", "LD E,E", "LD E,H", "LD E,L", "LD E,(HL)", "LD E,A",
			"LD H,B", "LD H,C", "LD H,D", "LD H,E", "LD H,H", "LD H,L", "LD H,(HL)", "LD H,A", "LD L,B", "LD L,C", "LD L,D", "LD L,E", "LD L,H", "LD L,L", "LD L,(HL)", "LD L,A",
			"LD (HL),B", "LD (HL),C", "LD (HL),D", "LD (HL),E", "LD (HL),H", "LD (HL),L", "HALT", "LD (HL),A", "LD A,B", "LD A,C", "LD A,D", "LD A,E", "LD A,H", "LD A,L", "LD A,(HL)", "LD A,A",
			"ADD A,B", "ADD A,C", "ADD A,D", "ADD A,E", "ADD A,H", "ADD A,L", "ADD A,(HL)", "ADD A,A", "ADC A,B", "ADC A,C", "ADC A,D", "ADC A,E", "ADC A,H", "ADC A,L", "ADC A,(HL)", "ADC A,A",
			"SUB B", "SUB C", "SUB D", "SUB E", "SUB H", "SUB L", "SUB (HL)", "SUB A", "SBC A,B", "SBC A,C", "SBC A,D", "SBC A,E", "SBC A,H", "SBC A,L", "SBC A,(HL)", "SBC A,A",
			"AND B", "AND C", "AND D", "AND E", "AND H", "AND L", "AND (HL)", "AND A", "XOR B", "XOR C", "XOR D", "XOR E", "XOR H", "XOR L", "XOR (HL)", "XOR A",
			"OR B", "OR C", "OR D", "OR E", "OR H", "OR L", "OR (HL)", "OR A", "CP B", "CP C", "CP D", "CP E", "CP H", "CP L", "CP (HL)", "CP A",
			"RET NZ", "POP BC", "JP NZ,a16", "JP a16", "CALL NZ,a16", "PUSH BC", "ADD A,d8", "RST 00H", "RET Z", "RET", "JP Z,a16", "CB", "CALL Z,a16", "CALL a16", "ADC A,d8", "RST 08H",
			"RET NC", "POP DE", "JP NC,a16", "Invalid", "CALL NC,a16", "PUSH DE", "SUB d8", "RST 10H", "RET C", "RETI", "JP C,a16", "Invalid", "CALL C,a16", "Invalid", "SBC A,d8", "RST 18H",
			"LDH (a8),A", "POP HL", "LD (C),A", "Invalid", "Invalid", "PUSH HL", "AND d8", "RST 20H", "ADD SP,r8", "JP (HL)", "LD (a16),A", "Invalid", "Invalid", "Invalid", "XOR d8", "RST 28H",
			"LDH A,(a8)", "POP AF", "LD A,(C)", "DI", "Invalid", "PUSH AF", "OR d8", "RST 30H", "LD HL,SP+r8", "LD SP,HL", "LD A,(a16)", "EI", "Invalid", "Invalid", "CP d8", "RST 38H"
		};

		public Emulator emu { get; private set; }

		void Awake()
		{
			emu = this.GetComponent<Emulator>();
		}

		public string OperationNameAtAddress(UInt16 addr)
		{
			byte op = emu.mmu.Read(addr);
			var code = opCodes[op];
			int parameters = ParametersForOpcode(op);
			if (parameters == 1) {
				code = string.Format("{0} 0x{1:X2}", code, emu.mmu.Read((UInt16)(addr + 1)));
			} else if (parameters == 2) {
				code = string.Format("{0} 0x{1:X4}", code, emu.mmu.ReadW((UInt16)(addr + 1)));
			}
			return code;
		}

		public int ParametersForOpcode(byte opcode)
		{
			byte[] oneParam = {
				0x06, 0x0E, 0x16, 0x1E, 0x26, 0xE0, 0xF0, 0xF8,
				0XE8, 0x18, 0x20, 0x28, 0x30, 0x38, 0xCB
			};

			byte[] twoParam = {
				0x7F, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x0A,
				0x1A, 0x7E, 0xFA, 0x3E, 0x7F, 0x47, 0x4f, 0x57, 
				0x5f, 0x67, 0x6f, 0x02, 0x12, 0x77, 0xEA, 0x01,
				0x11, 0x21, 0x31, 0x08, 0xC3, 0xC2, 0xCA, 0xD2,
				0xDA, 0xCD, 0xC4, 0xCC, 0xD4, 0xDC
			};


			var result = 0;
			foreach (byte b in oneParam) {
				if (b == opcode) {
					result = 1;
					break;
				}
			}

			if (result == 0) {
				foreach (byte b in twoParam) {
					if (b == opcode) {
						result = 2;
						break;
					}
				}
			}

			return result;
		}
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(DebugTools))]
	public class EmulatorEditor : Editor {

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (!EditorApplication.isPlaying)
				return;

			Color defaultColor = GUI.color;
			DebugTools debugTools = (target as DebugTools);
			Emulator emu = debugTools.emu;
			if (!emu.isOn) {
				GUI.color = Color.green;
				if (GUILayout.Button("Turn on")) {
					emu.TurnOn();
				}
				GUI.color = defaultColor;
			} else {
				GUI.color = Color.red;
				if (GUILayout.Button("Turn off")) {
					emu.TurnOff();
				}
				GUI.color = defaultColor;

				GUI.color = Color.yellow;
				if (GUILayout.Button("Reset")) {
					emu.Reset();
				}
				GUI.color = defaultColor;
			}


			if (emu.isOn) {
				GUILayout.Space(30);
				if (GUILayout.Button("Next step")) {
					emu.EmulatorStep();
				}

				GUILayout.Label(string.Format("AF: {0:X4}", emu.cpu.registers.AF)); 
				GUILayout.Label(string.Format("BC: {0:X4}", emu.cpu.registers.BC)); 
				GUILayout.Label(string.Format("DE: {0:X4}", emu.cpu.registers.DE)); 
				GUILayout.Label(string.Format("HL: {0:X4}", emu.cpu.registers.HL)); 
				GUILayout.Label(string.Format("Z: {0}, N: {1}, H:{2}, C:{3}", 
					emu.cpu.registers.flagZ?1:0, emu.cpu.registers.flagN?1:0, 
					emu.cpu.registers.flagH?1:0, emu.cpu.registers.flagC?1:0)); 
				GUILayout.Space(5);
				GUILayout.Label(string.Format("SP: {0:X4}", emu.cpu.registers.SP)); 
				GUILayout.Label(string.Format("PC: {0:X4}", emu.cpu.registers.PC)); 
				GUILayout.Space(5);
				GUILayout.Label(string.Format("t: {0}, m: {1}", 
					emu.cpu.timers.t, emu.cpu.timers.m));
				GUILayout.Space(5);
				GUILayout.Label(string.Format("stop: {0}, halt: {1}, ime: {2}", 
					emu.cpu.stop?1:0, emu.cpu.halt?1:0, emu.cpu.ime?1:0));
				GUILayout.Space(10);
				GUILayout.Label(string.Format("{0:X4} | {1:X2} | {2}", 
					emu.cpu.registers.PC, 
					emu.mmu.Read(emu.cpu.registers.PC), 
					debugTools.OperationNameAtAddress(emu.cpu.registers.PC)));
			}
		}
	}
	#endif
}
