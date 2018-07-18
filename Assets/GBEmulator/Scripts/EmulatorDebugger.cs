using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace brovador.GBEmulator {

	public class EmulatorDebugger : MonoBehaviour{

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

		string[] opCodesCB = {
			"RLC B", "RLC C", "RLC D", "RLC E", "RLC H", "RLC L", "RLC (HL)", "RLC A", "RRC B", "RRC C", "RRC D", "RRC E", "RRC H", "RRC L", "RRC (HL)", "RRC A",
			"RL B", "RL C", "RL D", "RL E", "RL H", "RL L", "RL (HL)", "RL A", "RR B", "RR C", "RR D", "RR E", "RR H", "RR L", "RR (HL)", "RR A",
			"SLA B", "SLA C", "SLA D", "SLA E", "SLA H", "SLA L", "SLA (HL)", "SLA A", "SRA B", "SRA C", "SRA D", "SRA E", "SRA H", "SRA L", "SRA (HL)", "SRA A",
			"SWAP B", "SWAP C", "SWAP D", "SWAP E", "SWAP H", "SWAP L", "SWAP (HL)", "SWAP A", "SRL B", "SRL C", "SRL D", "SRL E", "SRL H", "SRL L", "SRL (HL)", "SRL A",
			"BIT 0,B", "BIT 0,C", "BIT 0,D", "BIT 0,E", "BIT 0,H", "BIT 0,L", "BIT 0,(HL)", "BIT 0,A", "BIT 1,B", "BIT 1,C", "BIT 1,D", "BIT 1,E", "BIT 1,H", "BIT 1,L", "BIT 1,(HL)", "BIT 1,A",
			"BIT 2,B", "BIT 2,C", "BIT 2,D", "BIT 2,E", "BIT 2,H", "BIT 2,L", "BIT 2,(HL)", "BIT 2,A", "BIT 3,B", "BIT 3,C", "BIT 3,D", "BIT 3,E", "BIT 3,H", "BIT 3,L", "BIT 3,(HL)", "BIT 3,A",
			"BIT 4,B", "BIT 4,C", "BIT 4,D", "BIT 4,E", "BIT 4,H", "BIT 4,L", "BIT 4,(HL)", "BIT 4,A", "BIT 5,B", "BIT 5,C", "BIT 5,D", "BIT 5,E", "BIT 5,H", "BIT 5,L", "BIT 5,(HL)", "BIT 5,A",
			"BIT 6,B", "BIT 6,C", "BIT 6,D", "BIT 6,E", "BIT 6,H", "BIT 6,L", "BIT 6,(HL)", "BIT 6,A", "BIT 7,B", "BIT 7,C", "BIT 7,D", "BIT 7,E", "BIT 7,H", "BIT 7,L", "BIT 7,(HL)", "BIT 7,A",
			"RES 0,B", "RES 0,C", "RES 0,D", "RES 0,E", "RES 0,H", "RES 0,L", "RES 0,(HL)", "RES 0,A", "RES 1,B", "RES 1,C", "RES 1,D", "RES 1,E", "RES 1,H", "RES 1,L", "RES 1,(HL)", "RES 1,A",
			"RES 2,B", "RES 2,C", "RES 2,D", "RES 2,E", "RES 2,H", "RES 2,L", "RES 2,(HL)", "RES 2,A", "RES 3,B", "RES 3,C", "RES 3,D", "RES 3,E", "RES 3,H", "RES 3,L", "RES 3,(HL)", "RES 3,A",
			"RES 4,B", "RES 4,C", "RES 4,D", "RES 4,E", "RES 4,H", "RES 4,L", "RES 4,(HL)", "RES 4,A", "RES 5,B", "RES 5,C", "RES 5,D", "RES 5,E", "RES 5,H", "RES 5,L", "RES 5,(HL)", "RES 5,A",
			"RES 6,B", "RES 6,C", "RES 6,D", "RES 6,E", "RES 6,H", "RES 6,L", "RES 6,(HL)", "RES 6,A", "RES 7,B", "RES 7,C", "RES 7,D", "RES 7,E", "RES 7,H", "RES 7,L", "RES 7,(HL)", "RES 7,A",
			"SET 0,B", "SET 0,C", "SET 0,D", "SET 0,E", "SET 0,H", "SET 0,L", "SET 0,(HL)", "SET 0,A", "SET 1,B", "SET 1,C", "SET 1,D", "SET 1,E", "SET 1,H", "SET 1,L", "SET 1,(HL)", "SET 1,A",
			"SET 2,B", "SET 2,C", "SET 2,D", "SET 2,E", "SET 2,H", "SET 2,L", "SET 2,(HL)", "SET 2,A", "SET 3,B", "SET 3,C", "SET 3,D", "SET 3,E", "SET 3,H", "SET 3,L", "SET 3,(HL)", "SET 3,A",
			"SET 4,B", "SET 4,C", "SET 4,D", "SET 4,E", "SET 4,H", "SET 4,L", "SET 4,(HL)", "SET 4,A", "SET 5,B", "SET 5,C", "SET 5,D", "SET 5,E", "SET 5,H", "SET 5,L", "SET 5,(HL)", "SET 5,A",
			"SET 6,B", "SET 6,C", "SET 6,D", "SET 6,E", "SET 6,H", "SET 6,L", "SET 6,(HL)", "SET 6,A", "SET 7,B", "SET 7,C", "SET 7,D", "SET 7,E", "SET 7,H", "SET 7,L", "SET 7,(HL)", "SET 7,A"
		};


		int[] opCodeBytes = {
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

		public Emulator emu { get; private set; }
		public bool stop { get; private set; }
		public string[] breakPoints;

		Coroutine updateCoroutine;

		void Awake()
		{
			emu = this.GetComponent<Emulator>();
			emu.attachedDebugger = this;
		}


		public void OnEmulatorStepUpdate()
		{
			UInt16 addr = emu.cpu.registers.PC;
			string saddr = string.Format("0x{0:X4}", addr);
			List<string> breakPoints = new List<string>(this.breakPoints);
			if (breakPoints.Contains(saddr)) {
				emu.paused = true;
			}
		}


		public string OperationNameAtAddress(UInt16 addr)
		{
			byte op = emu.mmu.Read(addr);
			var code = opCodes[op];

			if (op != 0xCB) {
				int parameters = opCodeBytes[op];
				if (parameters == 2) {
					code = string.Format("{0} 0x{1:X2}", code, emu.mmu.Read((UInt16)(addr + 1)));
				} else if (parameters == 3) {
					code = string.Format("{0} 0x{1:X4}", code, emu.mmu.ReadW((UInt16)(addr + 1)));
				}
			} else {
				op = emu.mmu.Read((UInt16)(addr + 1));
				code = opCodesCB[op];
			}
			return code;
		}

		void OnGUI()
		{
			if (emu.isOn) {
				GUILayout.BeginVertical();
				GUILayout.Label(emu.paused ? string.Format("--- PAUSED AT: 0x{0:X4} ---", emu.cpu.registers.PC)  : "Running...");
				GUILayout.Label(string.Format("AF: {0:X4}", emu.cpu.registers.AF)); 
				GUILayout.Label(string.Format("BC: {0:X4}", emu.cpu.registers.BC)); 
				GUILayout.Label(string.Format("DE: {0:X4}", emu.cpu.registers.DE)); 
				GUILayout.Label(string.Format("HL: {0:X4}", emu.cpu.registers.HL)); 
				GUILayout.Label(string.Format("Z: {0}, N: {1}, H:{2}, C:{3}", 
					emu.cpu.registers.flagZ ? 1 : 0, emu.cpu.registers.flagN ? 1 : 0, 
					emu.cpu.registers.flagH ? 1 : 0, emu.cpu.registers.flagC ? 1 : 0)); 
				GUILayout.Space(5);
				GUILayout.Label(string.Format("SP: {0:X4}", emu.cpu.registers.SP)); 
				GUILayout.Label(string.Format("PC: {0:X4}", emu.cpu.registers.PC)); 
				GUILayout.Space(5);
				GUILayout.Label(string.Format("t: {0}", 
					emu.cpu.timers.t));
				GUILayout.Space(5);
				GUILayout.Label(string.Format("stop: {0}, halt: {1}, ime: {2}", 
					emu.cpu.stop ? 1 : 0, emu.cpu.halt ? 1 : 0, emu.cpu.ime ? 1 : 0));
				GUILayout.Space(10);
				GUILayout.Label(string.Format("{0:X4} | {1:X2} | {2}", 
					emu.cpu.registers.PC, 
					emu.mmu.Read(emu.cpu.registers.PC), 
					OperationNameAtAddress(emu.cpu.registers.PC)));
				GUILayout.EndVertical();


				GUILayout.BeginArea(new Rect(Screen.width / 2.0f, 0.0f, Screen.width / 2.0f, Screen.height));
				GUILayout.Label("Stack");
				for (UInt16 i = 0xFFFD; i >= emu.cpu.registers.SP; i-=2) {
					GUILayout.Label(string.Format("0x{0:X4} | 0x{1:X4}", i, emu.mmu.ReadW((UInt16)i)));
				}
				GUILayout.EndArea();
			}
		}
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(EmulatorDebugger))]
	public class EmulatorEditor : Editor {

		string addrToCheck = "";
		string resultAddr = "";

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (!EditorApplication.isPlaying)
				return;

			Color defaultColor = GUI.color;
			EmulatorDebugger debugTools = (target as EmulatorDebugger);
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


			if (emu.isOn && emu.paused) {
				GUILayout.Space(30);
				if (GUILayout.Button("Next step")) {
					emu.EmulatorStep();
				}
				if (GUILayout.Button("Resume")) {
					emu.paused = false;
				}

				GUILayout.Space(30);
				GUILayout.BeginHorizontal();
				addrToCheck = GUILayout.TextField(addrToCheck);
				if (GUILayout.Button("Check Address")) {
					if (addrToCheck != string.Empty) {
						UInt16 dir = (UInt16)(System.Convert.ToInt16(addrToCheck, 16));
						resultAddr = string.Format("Value: 0x{0:X4}", emu.mmu.ReadW(dir));
					}
				}
				GUILayout.EndHorizontal();

				if (resultAddr != string.Empty) {
					GUILayout.Label(resultAddr);
				}
			}
		}
	}
	#endif
}
