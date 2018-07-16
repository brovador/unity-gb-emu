using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace brovador.GBEmulator {
	
	public class Emulator : MonoBehaviour {
		
		Coroutine emulatorStepCoroutine;
		public bool isOn { get; private set; }

		[HideInInspector] public CPU cpu;
		[HideInInspector] public MMU mmu;

		void Init()
		{
			mmu = new MMU();
			cpu = new CPU(mmu);
		}

		#region Public

		public void TurnOn()
		{
			if (isOn) return;
			Init();
			//StartEmulatorCoroutine();
			isOn = true;
		}


		public void TurnOff()
		{
			if (!isOn) return;
			StopEmulatorCoroutine();
			isOn = false;
		}


		public void Reset()
		{
			TurnOff();
			TurnOn();
		}

		#endregion


		#region Private

		public void EmulatorStep()
		{
			cpu.Step();
		}


		void StartEmulatorCoroutine()
		{
			StopEmulatorCoroutine();
			emulatorStepCoroutine = StartCoroutine(EmulatorCoroutine());
		}


		void StopEmulatorCoroutine()
		{
			if (emulatorStepCoroutine != null) {
				StopCoroutine(emulatorStepCoroutine);
			}
		}


		IEnumerator EmulatorCoroutine()
		{
			while (true) {
				EmulatorStep();
				yield return null;
			}
		}

		#endregion
	}

	[CustomEditor(typeof(Emulator))]
	public class EmulatorEditor : Editor {

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (!EditorApplication.isPlaying)
				return;

			Color defaultColor = GUI.color;
			Emulator emu = target as Emulator;
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
				GUILayout.Label(string.Format("Next OP: {0:X2} {1:X2} {2:X2}", 
					emu.mmu.Read((UInt16)(emu.cpu.registers.PC)),
					emu.mmu.Read((UInt16)(emu.cpu.registers.PC + 1)),
					emu.mmu.Read((UInt16)(emu.cpu.registers.PC + 2))
				));
			}
		}
	}

}
