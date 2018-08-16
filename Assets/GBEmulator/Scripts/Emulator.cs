﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace brovador.GBEmulator {

	public class Emulator : MonoBehaviour {

		public event System.Action<Emulator> OnEmulatorOn;
		public event System.Action<Emulator> OnEmulatorOff;
		public event System.Action<Emulator> OnEmulatorStep;

		public const float FPS = 59.7f;
		public bool skipBios;
		public TextAsset rom;
		public Material outputMaterial;

		Coroutine emulatorStepCoroutine;
		public bool isOn { get; private set; }

		[HideInInspector] public bool paused = false;
		[HideInInspector] public CPU cpu;
		[HideInInspector] public MMU mmu;
		[HideInInspector] public GPU gpu;
		[HideInInspector] public Timer timer;

		void Init()
		{
			mmu = new MMU();
			cpu = new CPU(mmu);
			gpu = new GPU(mmu);
			timer = new Timer(mmu);

			if (outputMaterial != null) {
				outputMaterial.SetTexture("_MainTex", gpu.screenTexture);
			}
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

			if (OnEmulatorOn != null) {
				OnEmulatorOn(this);
			}
		}


		public void TurnOff()
		{
			if (!isOn) return;
			StopEmulatorCoroutine();
			isOn = false;

			if (OnEmulatorOff != null) {
				OnEmulatorOff(this);
			}
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
			var opCycles = cpu.Step();
			timer.Step(opCycles);
			gpu.Step(opCycles);
		}


		void SimulateBiosStartup()
		{
			cpu.registers.AF = 0x01B0; //0x01=GB/SGB, 0xFF=GBP, 0x11=GBC
			cpu.registers.BC = 0x0013;
			cpu.registers.DE = 0x00D8;
			cpu.registers.HL = 0x014D;
			cpu.registers.SP = 0xFFFE;
			cpu.registers.PC = 0x0100;

			//in bgb all IO starts in general with FF
			for (int i = 0xFF00; i < 0xFF4C; i++) {
				mmu.IOWrite((ushort)i, (byte)0xFF);
			}
			
			//IO default values
			mmu.IOWrite((ushort)0xFF01, (byte)0x00);
			mmu.IOWrite((ushort)0xFF02, (byte)0x7E);
			mmu.IOWrite((ushort)0xFF04, (byte)0xAB);
			mmu.IOWrite((ushort)0xFF05, (byte)0x00);
			mmu.IOWrite((ushort)0xFF06, (byte)0x00);
			mmu.IOWrite((ushort)0xFF07, (byte)0x00);
			mmu.IOWrite((ushort)0xFF0F, (byte)0xE1);

			mmu.IOWrite((ushort)0xFF10, (byte)0x80);
			mmu.IOWrite((ushort)0xFF11, (byte)0xBF);
			mmu.IOWrite((ushort)0xFF12, (byte)0xF3);
			mmu.IOWrite((ushort)0xFF14, (byte)0xBF);
			mmu.IOWrite((ushort)0xFF16, (byte)0x3F);
			mmu.IOWrite((ushort)0xFF17, (byte)0x00);
			mmu.IOWrite((ushort)0xFF19, (byte)0xBF);
			mmu.IOWrite((ushort)0xFF1A, (byte)0x7F);
			mmu.IOWrite((ushort)0xFF1B, (byte)0xFF);
			mmu.IOWrite((ushort)0xFF1C, (byte)0x9F);
			mmu.IOWrite((ushort)0xFF1E, (byte)0xBF);

			mmu.IOWrite((ushort)0xFF21, (byte)0x00);
			mmu.IOWrite((ushort)0xFF22, (byte)0x00);
			mmu.IOWrite((ushort)0xFF23, (byte)0xBF);
			mmu.IOWrite((ushort)0xFF24, (byte)0x77);
			mmu.IOWrite((ushort)0xFF25, (byte)0xF3);
			mmu.IOWrite((ushort)0xFF26, (byte)0xF1);

			mmu.IOWrite((ushort)0xFF40, (byte)0x91);
			mmu.IOWrite((ushort)0xFF41, (byte)0x85);
			mmu.IOWrite((ushort)0xFF42, (byte)0x00);
			mmu.IOWrite((ushort)0xFF43, (byte)0x00);
			mmu.IOWrite((ushort)0xFF44, (byte)0x00);
			mmu.IOWrite((ushort)0xFF45, (byte)0x00);
			mmu.IOWrite((ushort)0xFF47, (byte)0xFC);
			mmu.IOWrite((ushort)0xFF4A, (byte)0x00);
			mmu.IOWrite((ushort)0xFF4B, (byte)0x00);

			mmu.Write((ushort)0xFFFF, (byte)0x00);
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


		public float LastFrameTime { get; private set; }
		IEnumerator EmulatorCoroutine()
		{
			var cyclesPerSecond = cpu.clockSpeed / FPS;

			while (true) {
				var fTime = cpu.timers.t + cyclesPerSecond;
				var fStart = Time.realtimeSinceStartup;
				while (cpu.timers.t < fTime) {
					if (OnEmulatorStep != null) {
						OnEmulatorStep(this);
					}
					while (paused) {
						yield return null;
					}
					EmulatorStep();
				}
				yield return null;
				LastFrameTime = Time.realtimeSinceStartup - fStart;
			}
		}

		#endregion
	}
}
