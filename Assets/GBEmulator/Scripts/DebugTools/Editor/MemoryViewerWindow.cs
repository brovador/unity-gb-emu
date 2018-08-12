using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace brovador.GBEmulator.Debugger {
	public class MemoryViewerWindow : EditorWindow {

		[MenuItem("GBTools/Windows/MemoryViewer")]
		static void GetWindow()
		{
			EditorWindow window = EditorWindow.GetWindow<MemoryViewerWindow>("MemoryViewer");
			window.Show();
		}

		EmulatorDebugger debugger = null;
		List<string> addresses;
		Vector2 addressesScrollOffset = Vector2.zero;


		void OnInspectorUpdate()
		{
			this.Repaint();
		}


		void OnGUI()
		{
			if (debugger == null) {
				debugger = GameObject.FindObjectOfType<EmulatorDebugger>();
			}

			if (debugger == null) {
				GUILayout.Label("No debugger found");
				return;
			}

			List<int> addressesToRemove = new List<int>();
			if (addresses == null) {
				addresses = new List<string>();
			}

			GUILayoutOption[] emptyOptions = null;

			GUILayoutOption[] addressOptions = {
				GUILayout.Width(80)
			};

			GUILayoutOption[] deleteButtonOptions = {
				GUILayout.Width(30)
			};

			GUILayout.Space(10);
			GUILayout.BeginScrollView(addressesScrollOffset, GUI.skin.scrollView);
			for (int i = 0; i < addresses.Count; i++) {
				GUILayout.BeginHorizontal();
				addresses[i] = GUILayout.TextField(addresses[i], addressOptions);

				var value = "0x00";
				if (debugger != null && debugger.emu != null && debugger.emu.isOn) {
					value = string.Format("0x{0:X2}", debugger.emu.mmu.Read((ushort)System.Convert.ToUInt16(addresses[i], 16)));
				}
				GUILayout.Label(value, addressOptions);
				GUILayout.FlexibleSpace();
				var color = GUI.backgroundColor;
				GUI.backgroundColor = Color.red;
				if (GUILayout.Button("X", deleteButtonOptions)) {
					addressesToRemove.Add(i);
				}
				GUI.backgroundColor = color;
				GUILayout.EndHorizontal();
			}
			if (GUILayout.Button("Add", emptyOptions)) {
				addresses.Add("0");
			}
			GUILayout.EndScrollView();

			for (int i = 0; i < addressesToRemove.Count; i++) {
				var idx = addressesToRemove[addressesToRemove.Count - i - 1];
				addresses.RemoveAt(idx);
			}
		}
	}
}