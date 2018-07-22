using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace brovador.GBEmulator {

	public class Emulator : MonoBehaviour {

		public const float FPS = 59.7f;
		public bool skipBios;
		public TextAsset rom;

		Coroutine emulatorStepCoroutine;
		public bool isOn { get; private set; }

		[HideInInspector] public bool paused = false;
		[HideInInspector] public EmulatorDebugger attachedDebugger;
		[HideInInspector] public CPU cpu;
		[HideInInspector] public MMU mmu;
		[HideInInspector] public GPU gpu;

		void Init()
		{
			mmu = new MMU();
			cpu = new CPU(mmu);
			gpu = new GPU(cpu, mmu);
		}

		#region Public

		public void TurnOn()
		{
			if (isOn) return;
			Init();

			if (skipBios) {
				SimulateBiosStartup();
			}

			if (rom != null) {
				mmu.LoadRom(rom.bytes);
			}

			StartEmulatorCoroutine();
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
			gpu.Step();
		}


		void SimulateBiosStartup()
		{
			cpu.registers.AF = 0x01B0; //0x01=GB/SGB, 0xFF=GBP, 0x11=GBC
			cpu.registers.BC = 0x0013;
			cpu.registers.DE = 0x00D8;
			cpu.registers.HL = 0x014D;
			cpu.registers.SP = 0xFFFE;
			cpu.registers.PC = 0x0100;

			//Mark as bios read
			mmu.Write((UInt16)0xFF50, (byte)0x01);

			//Set default register values
			mmu.Write((UInt16)0xFF05, (byte)0x00);
			mmu.Write((UInt16)0xFF06, (byte)0x00);
			mmu.Write((UInt16)0xFF07, (byte)0x00);
			mmu.Write((UInt16)0xFF10, (byte)0x80);
			mmu.Write((UInt16)0xFF11, (byte)0xBF);
			mmu.Write((UInt16)0xFF12, (byte)0xF3);
			mmu.Write((UInt16)0xFF14, (byte)0xBF);
			mmu.Write((UInt16)0xFF16, (byte)0x3F);
			mmu.Write((UInt16)0xFF17, (byte)0x00);
			mmu.Write((UInt16)0xFF19, (byte)0xBF);
			mmu.Write((UInt16)0xFF1A, (byte)0x7F);
			mmu.Write((UInt16)0xFF1B, (byte)0xFF);
			mmu.Write((UInt16)0xFF1C, (byte)0x9F);
			mmu.Write((UInt16)0xFF1E, (byte)0xBF);
			mmu.Write((UInt16)0xFF20, (byte)0xFF);
			mmu.Write((UInt16)0xFF21, (byte)0x00);
			mmu.Write((UInt16)0xFF22, (byte)0x00);
			mmu.Write((UInt16)0xFF23, (byte)0xBF);
			mmu.Write((UInt16)0xFF24, (byte)0x77);
			mmu.Write((UInt16)0xFF25, (byte)0xF3);
			mmu.Write((UInt16)0xFF26, (byte)0xF1);
			mmu.Write((UInt16)0xFF40, (byte)0x91);
			mmu.Write((UInt16)0xFF42, (byte)0x00);
			mmu.Write((UInt16)0xFF43, (byte)0x00);
			mmu.Write((UInt16)0xFF45, (byte)0x00);
			mmu.Write((UInt16)0xFF47, (byte)0xFC);
			mmu.Write((UInt16)0xFF48, (byte)0xFF);
			mmu.Write((UInt16)0xFF49, (byte)0xFF);
			mmu.Write((UInt16)0xFF4A, (byte)0x00);
			mmu.Write((UInt16)0xFF4B, (byte)0x00);
			mmu.Write((UInt16)0xFFFF, (byte)0x00);
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
			var cyclesPerSecond = cpu.clockSpeed / FPS;

			while (true) {
				var fTime = cpu.timers.t + cyclesPerSecond;
				while (cpu.timers.t < fTime) {
					if (attachedDebugger != null) {
						attachedDebugger.OnEmulatorStepUpdate();
					}
					while (paused) {
						yield return null;
					}
					EmulatorStep();
				}
				yield return null;
			}
		}

		#endregion
	}
}
