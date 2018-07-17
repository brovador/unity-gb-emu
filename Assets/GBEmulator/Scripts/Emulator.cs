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
}
