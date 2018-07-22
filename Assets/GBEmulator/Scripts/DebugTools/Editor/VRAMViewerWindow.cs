using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using brovador.GBEmulator;

public class VRAMViewerWindow : EditorWindow {

	const int TOTAL_TILES = 32 * 32;

	[MenuItem("GBTools/Windows/VRAM Viewer")]
	static void GetWindow()
	{
		EditorWindow window = EditorWindow.GetWindow<VRAMViewerWindow>();
		window.Show();
	}


	Emulator emu = null;

	Texture2D[] textures;
	public void InitTextures(bool forced = false)
	{
		if (textures == null || textures.Length < TOTAL_TILES || forced) {
			textures = new Texture2D[TOTAL_TILES];
			for (int i = 0; i < textures.Length; i++) {
				var t = new Texture2D(8, 8, TextureFormat.ARGB32, false);
				for (int j = 0; j < 64; j++) {
					t.SetPixel(j / 8, j % 8, Color.black);
					t.Apply();
				}
				textures[i] = t;
			}
		}
	}


	void OnGUI()
	{
		//emu = EditorGUILayout.ObjectField("Emulator: ", emu, typeof(Emulator), true) as Emulator;
		if (emu == null) {
			emu = GameObject.FindObjectOfType<Emulator>();
		}

		if (emu == null || !emu.isOn || !Application.isPlaying) return;

		InitTextures();
		if (GUILayout.Button("Refresh")) {
			InitTextures(true);
			for (int i = 0; i < TOTAL_TILES; i++) {
				UpdateTileTexture(textures[i], (uint)(0x8000 + 16 * i));
			}
		}


		GUIStyle buttonStyle = new GUIStyle(GUI.skin.label);
		buttonStyle.margin = new RectOffset(0, 0, 0, 0);
		buttonStyle.contentOffset = Vector2.zero;
		buttonStyle.stretchWidth = true;
		buttonStyle.stretchHeight = true;
		buttonStyle.overflow = new RectOffset(10, 10, 10, 10);
//		buttonStyle.fixedWidth = 0;
//		buttonStyle.fixedHeight = 0;

		GUILayout.BeginVertical();
		for (int i = 0; i < 16; i++) {
			GUILayout.BeginHorizontal();
			for (int j = 0; j < 16; j++) {
				GUILayoutOption[] options = {
					GUILayout.Width(8), GUILayout.Height(8)
				};
				Texture2D t = textures[i * 16 + j];
				GUILayout.Label(t, buttonStyle);
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
	}


	void UpdateTileTexture(Texture2D t, uint addr)
	{
		Color[] colors = {
			Color.black,
			Color.white,
			Color.yellow,
			Color.green
		};

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

			for (int c = 0; c < 8; c++) {
				t.SetPixel(c, 7 - i, line[c]);
			}
			t.Apply();
		}
	}
}
