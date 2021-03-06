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
		public TextAsset rom;
		public Material outputMaterial;

		Coroutine emulatorStepCoroutine;
		public bool isOn { get; private set; }

		[HideInInspector] public bool paused = false;
		[HideInInspector] public CPU cpu;
		[HideInInspector] public MMU mmu;
		[HideInInspector] public GPU gpu;
		[HideInInspector] public Timer timer;
		[HideInInspector] public Joypad joypad;
		bool skipBios = true;

		void Init()
		{
			mmu = new MMU();
			cpu = new CPU(mmu);
			gpu = new GPU(mmu);
			timer = new Timer(mmu);
			joypad = new Joypad(mmu);

			if (outputMaterial != null) {
				outputMaterial.SetTexture("_MainTex", gpu.screenTexture);
			}

			InitKeyMap();
		}


		void Update()
		{
			if (isOn) {
				EmulatorFrame();
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

			isOn = true;

			if (OnEmulatorOn != null) {
				OnEmulatorOn(this);
			}
		}


		public void TurnOff()
		{
			if (!isOn) return;
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

		public void EmulatorFrame()
		{
			if (paused) return;

			CheckKeys();

			var cyclesPerFrame = cpu.clockSpeed / FPS;
			var fTime = cpu.timers.t + cyclesPerFrame;

			while (cpu.timers.t < fTime) {
				if (OnEmulatorStep != null) {
					OnEmulatorStep(this);
				}

				if (!paused) {
					EmulatorStep();
				} else {
					break;
				}
			}
		}


		public void EmulatorStep(bool frameskip = false)
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

			//IO default values
			mmu.Write((ushort)0xFF01, (byte)0x00);
			mmu.Write((ushort)0xFF02, (byte)0x7E);
			mmu.Write((ushort)0xFF04, (byte)0xAB);
			mmu.Write((ushort)0xFF05, (byte)0x00);
			mmu.Write((ushort)0xFF06, (byte)0x00);
			mmu.Write((ushort)0xFF07, (byte)0x00);
			mmu.Write((ushort)0xFF0F, (byte)0xE1);

			mmu.Write((ushort)0xFF10, (byte)0x80);
			mmu.Write((ushort)0xFF11, (byte)0xBF);
			mmu.Write((ushort)0xFF12, (byte)0xF3);
			mmu.Write((ushort)0xFF14, (byte)0xBF);
			mmu.Write((ushort)0xFF16, (byte)0x3F);
			mmu.Write((ushort)0xFF17, (byte)0x00);
			mmu.Write((ushort)0xFF19, (byte)0xBF);
			mmu.Write((ushort)0xFF1A, (byte)0x7F);
			mmu.Write((ushort)0xFF1B, (byte)0xFF);
			mmu.Write((ushort)0xFF1C, (byte)0x9F);
			mmu.Write((ushort)0xFF1E, (byte)0xBF);

			mmu.Write((ushort)0xFF21, (byte)0x00);
			mmu.Write((ushort)0xFF22, (byte)0x00);
			mmu.Write((ushort)0xFF23, (byte)0xBF);
			mmu.Write((ushort)0xFF24, (byte)0x77);
			mmu.Write((ushort)0xFF25, (byte)0xF3);
			mmu.Write((ushort)0xFF26, (byte)0xF1);

			mmu.Write((ushort)0xFF40, (byte)0x91);
			mmu.Write((ushort)0xFF41, (byte)0x85);
			mmu.Write((ushort)0xFF42, (byte)0x00);
			mmu.Write((ushort)0xFF43, (byte)0x00);
			mmu.Write((ushort)0xFF44, (byte)0x00);
			mmu.Write((ushort)0xFF45, (byte)0x00);
			mmu.Write((ushort)0xFF47, (byte)0xFC);
			mmu.Write((ushort)0xFF4A, (byte)0x00);
			mmu.Write((ushort)0xFF4B, (byte)0x00);

			mmu.Write((ushort)0xFF50, (byte)0x01);

			mmu.Write((ushort)0xFFFF, (byte)0x00);
		}

		#endregion

		Dictionary<KeyCode, Joypad.Button> keyMap;
		List<KeyCode> keys;

		void InitKeyMap()
		{
			keyMap = new Dictionary<KeyCode, Joypad.Button>();
			keyMap[KeyCode.LeftArrow] = Joypad.Button.Left;
			keyMap[KeyCode.RightArrow] = Joypad.Button.Right;
			keyMap[KeyCode.UpArrow] = Joypad.Button.Up;
			keyMap[KeyCode.DownArrow] = Joypad.Button.Down;
			keyMap[KeyCode.A] = Joypad.Button.A;
			keyMap[KeyCode.S] = Joypad.Button.B;
			keyMap[KeyCode.Return] = Joypad.Button.Start;
			keyMap[KeyCode.Delete] = Joypad.Button.Select;

			keys = new List<KeyCode>();
			foreach (var kv in keyMap) {
				keys.Add(kv.Key);
			}
		}

		void CheckKeys()
		{
			for (int i = 0; i < keys.Count; i++) {
				var key = keys[i];
				if (Input.GetKey(key)) {
					joypad.SetKey(keyMap[key], true);	
				} else {
					joypad.SetKey(keyMap[key], false);	
				}
			}
		}
	}
}
