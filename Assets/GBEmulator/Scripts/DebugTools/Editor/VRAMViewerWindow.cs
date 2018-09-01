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


		int selectedView = 0;

		Dictionary<uint, Color[]> tiles = new Dictionary<uint, Color[]>();
		Texture2D vramTexture;
		Texture2D tilesTexture;

		void OnGUI()
		{
			//emu = EditorGUILayout.ObjectField("Emulator: ", emu, typeof(Emulator), true) as Emulator;
			if (emu == null) {
				emu = GameObject.FindObjectOfType<Emulator>();
			}

			string[] optionTitles = {
				"BG Map", "Tiles"
			};
			GUILayoutOption[] options = null;
			selectedView = GUILayout.Toolbar(selectedView, optionTitles, options);

			if (emu == null || !emu.isOn || !Application.isPlaying) {
				return;
			}

			if (selectedView == 0) {
				ShowBGMap();
			} else if (selectedView == 1) {
				ShowTiles();
			}
		}


		void OnInspectorUpdate()
		{
			this.Repaint();
		}


		void ShowBGMap()
		{
			UpdateTilesDict();
			if (vramTexture == null) {
				vramTexture = new Texture2D(32 * 8, 32 * 8, TextureFormat.ARGB32, false);
				vramTexture.filterMode = FilterMode.Point;
			}
			var addr = emu.gpu.LCDC_BGTileMap == 0 ? 0x9800 : 0x9C00;
			for (int i = 0; i < 32; i++) {
				for (int j = 0; j < 32; j++) {

					int n = (int)emu.mmu.Read((ushort)(addr + i * 32 + j));
					if (emu.gpu.LCDC_BGWindowTileData == 0) {
						if (n > 127) {
							n -= 0x100;
						}
						n = 256 + n;
					}

					Color[] tile = tiles[(uint)n];
					vramTexture.SetPixels(j * 8, (31 - i) * 8, 8, 8, tile);
				}
			}
			vramTexture.Apply();
			DrawTexture(vramTexture, 32);
		}


		void ShowTiles()
		{
			UpdateTilesDict();
			if (tilesTexture == null) {
				tilesTexture = new Texture2D(32 * 8, 32 * 8, TextureFormat.ARGB32, false);
				tilesTexture.filterMode = FilterMode.Point;
			}
			for (int i = 0; i < 32; i++) {
				for (int j = 0; j < 16; j++) {
					Color[] tile = tiles[(uint)(i * 16 + j)];
					tilesTexture.SetPixels(j * 8, (31 - i) * 8, 8, 8, tile); 
				}
			}
			tilesTexture.Apply();
			DrawTexture(tilesTexture, 32);
		}


		void DrawTexture(Texture2D t, int sizeW, int sizeH = 0, int textureScale = 8)
		{
			if (sizeH == 0) {
				sizeH = sizeW;
			}

			sizeW = sizeW * textureScale;
			sizeH = sizeH * textureScale;


			GUIStyle buttonStyle = new GUIStyle(GUI.skin.label);
			buttonStyle.margin = new RectOffset(0, 0, 0, 0);

			GUILayoutOption[] options = {
				GUILayout.Width(sizeW), GUILayout.Height(sizeH)
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

		void UpdateTilesDict()
		{
			tiles.Clear();

			for (int n = 0; n < TOTAL_TILES; n++) {
				var addr = (uint)(0x8000 + 16 * n);
				Color[] tile = new Color[8 * 8];
				for (int i = 0; i < 8; i++) {
					byte b1 = emu.mmu.Read((ushort)(addr + i * 2));
					byte b2 = emu.mmu.Read((ushort)(addr + i * 2 + 1));

					tile[(7 - i) * 8] = colors[(int)((b1 & 0x80) >> 7) + (int)((b2 & 0x80) >> 6)];
					tile[(7 - i) * 8 + 1] = colors[(int)((b1 & 0x40) >> 6) + (int)((b2 & 0x40) >> 5)];
					tile[(7 - i) * 8 + 2] = colors[(int)((b1 & 0x20) >> 5) + (int)((b2 & 0x20) >> 4)];
					tile[(7 - i) * 8 + 3] = colors[(int)((b1 & 0x10) >> 4) + (int)((b2 & 0x10) >> 3)];
					tile[(7 - i) * 8 + 4] = colors[(int)((b1 & 0x08) >> 3) + (int)((b2 & 0x08) >> 2)];
					tile[(7 - i) * 8 + 5] = colors[(int)((b1 & 0x04) >> 2) + (int)((b2 & 0x04) >> 1)];
					tile[(7 - i) * 8 + 6] = colors[(int)((b1 & 0x02) >> 1) + (int)((b2 & 0x02))];
					tile[(7 - i) * 8 + 7] = colors[(int)((b1 & 0x01)) + (int)((b2 & 0x01) << 1)];
				}
				tiles[(uint)n] = tile;
			}
		}
	}
}