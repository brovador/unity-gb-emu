﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace brovador.GBEmulator {
	
	public class GPU {
		
		public const int SCREEN_PIXELS_WIDTH = 160;
		public const int SCREEN_PIXELS_HEIGHT = 144;

		const int HORIZONAL_BLANK_CYCLES = 204;
		const int VERTICAL_BLANK_CYCLES = 4560;
		const int SCANLINE_OAM_CYCLES = 80;
		const int SCANLINE_VRAM_CYCLES = 172;

		const int TOTAL_TILES = 512;
		
		enum GPUMode {
			HBlank,
			VBlank,
			OAMRead,
			VRAMRead
		}

		public enum SpriteSize {
			Size8x8,
			Size8x16
		}

		MMU mmu;

		//FF40(LCDC)
		public int LCDC_BGTileMap { get { return (mmu.Read(0xFF40) & 0x08) == 0 ? 0 : 1; } }
		public int LCDC_WindowTileMap { get { return (mmu.Read(0xFF40) & 0x40) == 0 ? 0 : 1; } }
		public int LCDC_BGWindowTileData { get { return (mmu.Read(0xFF40) & 0x10) == 0 ? 0 : 1; } }

		public bool LCDC_WindowDisplay { get { return (mmu.Read(0xFF40) & 0x20) == 0 ? false : true; } }
		public bool LCDC_SpriteDisplay { get { return (mmu.Read(0xFF40) & 0x02) == 0 ? false : true; } }
		public bool LCDC_BGWindowDisplay { get { return (mmu.Read(0xFF40) & 0x01) == 0 ? false : true; } }

		public SpriteSize LCDC_SpriteSize { get { return (SpriteSize)(mmu.Read(0xFF40) & 0x04); } }

		//FF41(STAT)
		#warning STAT bits 6-3 not set
		#warning STAT coincicende flag not set
		GPUMode STAT_Mode {
			get { return (GPUMode)(mmu.Read((ushort)0xFF41) & 0x03); }
			set { mmu.IOWrite(0xFF41, (byte)((mmu.Read(0xFF41) & ~0x03) + (byte)value)); }
		}

		//FF42(SCY)
		byte SCY { get { return (byte)(mmu.Read(0xFF42) & 0x0F); } }

		//FF43(SCX)
		byte SCX { get { return (byte)(mmu.Read(0xFF43) & 0x0F); } }

		//FF44(LY)
		byte LY { 
			get { return mmu.Read(0xFF44); } 
			set { mmu.IOWrite(0xFF44, value); } 
		}

		//FF45(LYC)
		#warning LYC set STAT when LY==LYC
		byte LYC { get { return mmu.Read(0xFF45); } }

		//FF46(DMA)
		byte DMA { get { return mmu.Read(0xFF46); } }

		//FF47(BGP)
		byte BGP { get { return mmu.Read(0xFF47); } }

		//FF48(OBP0)
		byte OBP0 { get { return mmu.Read(0xFF48); } }

		//FF49(OBP1)
		byte OBP1 { get { return mmu.Read(0xFF49); } }

		//FF4A(WY)
		byte WY { get { return mmu.Read(0xFF4A); } }

		//FF4B(WX)
		byte WX { get { return mmu.Read(0xFF4B); } }

		uint clock;

		Color[] buffer;
		public Texture2D screenTexture { get; private set; }

		public GPU(MMU mmu) 
		{
			this.mmu = mmu;
			this.mmu.OnMemoryAccess += (MMU arg1, ushort arg2, bool arg3) => {
				if (arg3 && arg2 >= 0x8000 && arg2 <= 0x97FF) {
					UpdateTile(arg2);
				}
			};

			STAT_Mode = GPUMode.HBlank;
			LY = 0;
			clock = 0;
			buffer = new Color[SCREEN_PIXELS_WIDTH * SCREEN_PIXELS_HEIGHT];

			screenTexture = new Texture2D(SCREEN_PIXELS_WIDTH, SCREEN_PIXELS_HEIGHT, TextureFormat.ARGB32, false);
			screenTexture.filterMode = FilterMode.Point;
		}


		public void Step(uint opCycles)
		{
			clock += opCycles;

			switch (STAT_Mode) {

			//HBlank
			case GPUMode.HBlank:
				if (clock >= HORIZONAL_BLANK_CYCLES) {
					clock -= (uint)HORIZONAL_BLANK_CYCLES;
					LY++;

					if (LY == (SCREEN_PIXELS_HEIGHT - 1)) {
						STAT_Mode = GPUMode.VBlank;
						mmu.SetInterrupt(MMU.InterruptType.VBlank);
						DrawScreen();
					} else {
						STAT_Mode = GPUMode.OAMRead;
					}
				}
				break;
			
			//VBlank
			case GPUMode.VBlank:
				uint t = (uint)(SCANLINE_OAM_CYCLES + SCANLINE_VRAM_CYCLES + HORIZONAL_BLANK_CYCLES);
				if (clock >= t) {
					clock -= t;
					LY++;

					if (LY > 153) {
						STAT_Mode = GPUMode.OAMRead;
						LY = 0;
					}
				}
				break;
			
			//OAM Read
			case GPUMode.OAMRead:
				if (clock >= SCANLINE_OAM_CYCLES) {
					clock -= (uint)SCANLINE_OAM_CYCLES;
					STAT_Mode = GPUMode.VRAMRead;
				}
				break;
			
			//VRAM Read
			case GPUMode.VRAMRead:
				if (clock >= SCANLINE_VRAM_CYCLES) {
					clock -= (uint)SCANLINE_VRAM_CYCLES;
					STAT_Mode = GPUMode.HBlank;
					DrawScanline();
				}
				break;
			}
		}


//		Color[] colors = {
//			new Color(224.0f / 255.0f, 248.0f / 255.0f, 208.0f / 255.0f),
//			new Color(136.0f / 255.0f, 192.0f / 255.0f, 112.0f / 255.0f),
//			new Color(52.0f / 255.0f, 104.0f / 255.0f, 86.0f / 255.0f),
//			new Color(8.0f / 255.0f, 24.0f / 255.0f, 32.0f / 255.0f)
//		};

		void DrawScanline()
		{
			//VRAM size is 32x32 tiles, 1 byte per tile
			var tileMapAddressOffset = LCDC_BGTileMap == 0 ? 0x9800 : 0x9C00;

			var lineY = LY + SCY;
			var lineX = SCX;
			var bufferY = (SCREEN_PIXELS_WIDTH * SCREEN_PIXELS_HEIGHT - (LY * SCREEN_PIXELS_WIDTH)) - SCREEN_PIXELS_WIDTH;

			for (int i = 0; i < SCREEN_PIXELS_WIDTH; i++) {

				lineX++;

				var tileMapY = (int)((lineY / 8) % 32);
				var tileMapX = (int)((lineX / 8) % 32);

				var tileY = (int)(lineY % 8);
				var tileX = (int)(lineX % 8);

				int nTile = mmu.Read((ushort)(tileMapAddressOffset + (tileMapY * 32) + tileMapX));
				if (LCDC_BGWindowTileData == 0) {
					if (nTile > 127) {
						nTile -= 0x100;
					}
					nTile = 256 + nTile;
				}

				if (tiles.ContainsKey((uint)nTile)) {
					var tile = tiles[(uint)nTile];
					buffer[bufferY + i] = colors[tile[tileY * 8 + tileX]];
				}
			}
		}

		void DrawScreen()
		{
			screenTexture.SetPixels(0, 0, SCREEN_PIXELS_WIDTH, SCREEN_PIXELS_HEIGHT, buffer);
			screenTexture.Apply();
		}


		Color[] colors = {
			new Color(224.0f / 255.0f, 248.0f / 255.0f, 208.0f / 255.0f),
			new Color(136.0f / 255.0f, 192.0f / 255.0f, 112.0f / 255.0f),
			new Color(52.0f / 255.0f, 104.0f / 255.0f, 86.0f / 255.0f),
			new Color(8.0f / 255.0f, 24.0f / 255.0f, 32.0f / 255.0f)
		};

		Dictionary<uint, int[]> tiles = new Dictionary<uint, int[]>();
		void UpdateTile(uint addr)
		{
			var n = (addr - 0x8000) / 16;
			var tileBaseAddress = 0x8000 + n * 16;
			var tileRow = (addr - tileBaseAddress) / 2;
			var tileRowAddr = tileBaseAddress + tileRow * 2;
			
			if (!tiles.ContainsKey(n)) {
				tiles[n] = new int[8 * 8];
			}

			byte b1 = mmu.Read((ushort)(tileRowAddr));
			byte b2 = mmu.Read((ushort)(tileRowAddr + 1));

			int[] tile = tiles[n];
			tile[tileRow * 8] = (int)((b1 & 0x80) >> 7) + (int)((b2 & 0x80) >> 6);
			tile[tileRow * 8 + 1] = (int)((b1 & 0x40) >> 6) + (int)((b2 & 0x40) >> 5);
			tile[tileRow * 8 + 2] = (int)((b1 & 0x20) >> 5) + (int)((b2 & 0x20) >> 4);
			tile[tileRow * 8 + 3] = (int)((b1 & 0x10) >> 4) + (int)((b2 & 0x10) >> 3);
			tile[tileRow * 8 + 4] = (int)((b1 & 0x08) >> 3) + (int)((b2 & 0x08) >> 2);
			tile[tileRow * 8 + 5] = (int)((b1 & 0x04) >> 2) + (int)((b2 & 0x04) >> 1);
			tile[tileRow * 8 + 6] = (int)((b1 & 0x02) >> 1) + (int)((b2 & 0x02));
			tile[tileRow * 8 + 7] = (int)((b1 & 0x01)) + (int)((b2 & 0x01) << 1);
		}
	}
}
