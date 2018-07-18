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
		public EmulatorDebugger attachedDebugger;

		Coroutine emulatorStepCoroutine;
		public bool isOn { get; private set; }
		public bool paused = false;

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
					while (paused) {
						yield return null;
					}
					EmulatorStep();
					if (attachedDebugger != null) {
						attachedDebugger.OnEmulatorStepUpdate();
					}
				}
				yield return null;
			}
		}

		#endregion
	}
}
