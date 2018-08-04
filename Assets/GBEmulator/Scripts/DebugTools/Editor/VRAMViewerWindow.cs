using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace brovador.GBEmulator.Debugger {

public class VRAMViewerWindow : EditorWindow {

	const int TOTAL_TILES = 32 * 32;

	[MenuItem("GBTools/Windows/VRAM Viewer")]
	static void GetWindow()
	{
		EditorWindow window = EditorWindow.GetWindow<VRAMViewerWindow>("VRAM Viewer");
		window.Show();
	}


	Emulator emu = null;

	Texture2D[] tilesTextures;
	public void InitTextures(bool forced = false)
	{
		if (tilesTextures == null || tilesTextures.Length < TOTAL_TILES || forced) {
			tilesTextures = new Texture2D[TOTAL_TILES];
			for (int i = 0; i < tilesTextures.Length; i++) {
				var t = new Texture2D(8, 8, TextureFormat.ARGB32, false);
				for (int j = 0; j < 64; j++) {
					t.SetPixel(j / 8, j % 8, Color.black);
					t.Apply();
				}
				tilesTextures[i] = t;
			}
		}
	}

	public int selectedView = 0;

	void OnGUI()
	{
		//emu = EditorGUILayout.ObjectField("Emulator: ", emu, typeof(Emulator), true) as Emulator;
		if (emu == null) {
			emu = GameObject.FindObjectOfType<Emulator>();
		}

		string[] optionTitles = {
			"BG Map", "Tiles", "OAM", "Palettes"
		};
		GUILayoutOption[] options = null;
		selectedView = GUILayout.Toolbar(selectedView, optionTitles, options);

		if (emu == null || !emu.isOn || !Application.isPlaying) {
			tilesTextures = null;
			return;
		}

		if (selectedView == 0) {
			ShowBGMap();
		} else if (selectedView == 1) {
			ShowTiles();
		}
	}


	void ShowBGMap()
	{
		int tilePreviewSize = 8;
		int backgroundSize = 32;

		InitTextures();
		UpdateTilesTextures();

		GUILayout.BeginVertical();
		var addr = 0x9800;
		for (int i = 0; i < backgroundSize; i++) {
			GUILayout.BeginHorizontal();
			for (int j = 0; j < backgroundSize; j++) {
				Texture2D t = tilesTextures[emu.mmu.Read((ushort)(addr + i * backgroundSize + j))];
				DrawTexture(tilePreviewSize, t);
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
	}


	void ShowTiles ()
	{
		int tilePreviewSize = 8;
		int tilesPerRow = 16;

		InitTextures();
		UpdateTilesTextures();
		
		GUILayout.BeginVertical();
		for (int i = 0; i < tilesPerRow; i++) {
			GUILayout.BeginHorizontal();
			for (int j = 0; j < tilesPerRow; j++) {
				Texture2D t = tilesTextures[i * tilesPerRow + j];
				DrawTexture(tilePreviewSize, t);
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
	}


	void DrawTexture(int size, Texture2D t)
	{
		GUIStyle buttonStyle = new GUIStyle(GUI.skin.label);
		buttonStyle.margin = new RectOffset(1, 1, 1, 1);

		GUILayoutOption[] options = {
			GUILayout.Width(size), GUILayout.Height(size)
		};

		GUILayout.Box("", buttonStyle, options);
		var rect = GUILayoutUtility.GetLastRect();
		GUI.DrawTexture(rect, t); 
	}

	Color[] colors = {
		new Color(224.0f / 255.0f, 248.0f / 255.0f, 208.0f / 255.0f),
		new Color(136.0f / 255.0f, 192.0f / 255.0f, 112.0f / 255.0f),
		new Color(52.0f / 255.0f, 104.0f / 255.0f, 86.0f / 255.0f),
		new Color(8.0f / 255.0f, 24.0f / 255.0f, 32.0f / 255.0f)
	};

	void UpdateTilesTextures()
	{
		for (int n = 0; n < TOTAL_TILES; n++) {
			var addr = (uint)(0x8000 + 16 * n);
			var tex = tilesTextures[n];

			for (int i = 0; i < 8; i++) {
				byte b1 = emu.mmu.Read((ushort)(addr + i * 2));
				byte b2 = emu.mmu.Read((ushort)(addr + i * 2 + 1));

				Color[] line = new Color[8];
				line[0] = colors[(int)((b1 & 0x80) >> 7) + (int)((b2 & 0x80) >> 7)];
				line[1] = colors[(int)((b1 & 0x40) >> 6) + (int)((b2 & 0x40) >> 6)];
				line[2] = colors[(int)((b1 & 0x20) >> 5) + (int)((b2 & 0x20) >> 5)];
				line[3] = colors[(int)((b1 & 0x10) >> 4) + (int)((b2 & 0x10) >> 4)];
				line[4] = colors[(int)((b1 & 0x08) >> 3) + (int)((b2 & 0x08) >> 3)];
				line[5] = colors[(int)((b1 & 0x04) >> 2) + (int)((b2 & 0x04) >> 2)];
				line[6] = colors[(int)((b1 & 0x02) >> 1) + (int)((b2 & 0x02) >> 1)];
				line[7] = colors[(int)((b1 & 0x01)) + (int)((b2 & 0x01))];

				tex.SetPixels(0, 7 - i, line.Length, 1, line);
				tex.Apply();
			}
		}
	}
}
}