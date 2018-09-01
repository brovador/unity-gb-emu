using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using brovador.GBEmulator;


namespace brovador.GBEmulator.Debugger {

	public class BreakpointsWindow : EditorWindow {

		[MenuItem("GBTools/Windows/Breakpoints")]
		static void GetWindow()
		{
			EditorWindow window = EditorWindow.GetWindow<BreakpointsWindow>("Breakpoints");
			window.Show();
		}

		EmulatorDebugger debugger = null;


		int selectedView = 0; // 0 breakpoints, 1 memory breakpoints
		Vector2 breakpointsScrollOffset = Vector2.zero;

		void OnGUI()
		{
			if (debugger == null) {
				debugger = GameObject.FindObjectOfType<EmulatorDebugger>();
			}

			if (debugger == null) {
				GUILayout.Label("No emulator debugger found");
				return;
			}

			GUILayoutOption[] options = null;

			GUILayout.Space(10);
			debugger.enableBreakPoints = GUILayout.Toggle(debugger.enableBreakPoints, "Enable breakpoints", options);
			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			string[] texts = {
				"Breakpoints", "Memory breakpoints"
			};
			selectedView = GUILayout.Toolbar(selectedView, texts, options);
			GUILayout.EndHorizontal();

			if (selectedView == 0) {
				DrawBreakpointsView(debugger.breakPoints);
			} else if (selectedView == 1) {
				DrawBreakpointsView(debugger.memoryBreakPoints);
			}
		}


		void DrawBreakpointsView(List<Breakpoint> breakpoints)
		{
			var bpToRemove = new List<Breakpoint>();

			GUIStyle style = new GUIStyle(GUI.skin.scrollView);
			GUILayoutOption[] options = null;

			GUIStyle contentLabelStyle = new GUIStyle(GUI.skin.label);
			contentLabelStyle.alignment = TextAnchor.MiddleRight;

			GUILayoutOption[] addressOptions = {
				GUILayout.Width(80)
			};

			GUILayoutOption[] toggleOptions = {
				GUILayout.Width(15)
			};
			GUILayoutOption[] enumOptions = {
				GUILayout.Width(80)
			};
			GUILayoutOption[] deleteOptions = {
				GUILayout.Width(30)
			};

			GUILayout.Space(10);

			breakpointsScrollOffset = GUILayout.BeginScrollView(breakpointsScrollOffset, style);

			foreach (Breakpoint bp in breakpoints) {
				
				GUILayout.BeginHorizontal();

				bp.active = GUILayout.Toggle(bp.active, "", toggleOptions);
				bp.address = GUILayout.TextField(bp.address, addressOptions);
				GUILayout.Label("Condition", contentLabelStyle, options);
				bp.condition = (Breakpoint.Condition)EditorGUILayout.EnumPopup(bp.condition, enumOptions);
				bp.conditionValue = GUILayout.TextField(bp.conditionValue, addressOptions);

				var color = GUI.backgroundColor;
				GUI.backgroundColor = Color.red;
				if (GUILayout.Button("X", deleteOptions)) {
					bpToRemove.Add(bp);
				}
				GUI.backgroundColor = color;

				GUILayout.EndHorizontal();
			}

			GUILayout.Space(10);
			if (GUILayout.Button("Add")) {
				breakpoints.Add(new Breakpoint());
			}
			GUILayout.EndScrollView();

			foreach (Breakpoint bp in bpToRemove) {
				breakpoints.Remove(bp);
			}
		}
	}
}
