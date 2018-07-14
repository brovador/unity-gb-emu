using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace brovador.GBEmulator {
	
	public class Emulator : MonoBehaviour {

		Coroutine emulatorStepCoroutine;

		CPU cpu;
		MMU mmu;

		void Init()
		{
			mmu = new MMU();
			cpu = new CPU(mmu);
		}


		#region Public

		public void TurnOn()
		{
			Init();
			StartEmulatorCoroutine();
		}


		public void TurnOff()
		{
			StopEmulatorCoroutine();
		}


		public void Reset()
		{
			TurnOff();
			TurnOn();
		}

		#endregion


		#region Private

		void EmulatorStep()
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
