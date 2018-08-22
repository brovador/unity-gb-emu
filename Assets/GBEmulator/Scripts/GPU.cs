using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace brovador.GBEmulator {
	
	public class GPU {
		
		public const int SCREEN_PIXELS_WIDTH = 160;
		public const int SCREEN_PIXELS_HEIGHT = 144;

		const int HORIZONAL_BLANK_CYCLES = 204;
		const int VERTICAL_BLANK_CYCLES = 456;
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
			set { mmu.Write(0xFF41, (byte)((mmu.Read(0xFF41) & ~0x03) + (byte)value)); }
		}

		//FF42(SCY)
		byte SCY { get { return (byte)(mmu.Read(0xFF42) & 0x0F); } }

		//FF43(SCX)
		byte SCX { get { return (byte)(mmu.Read(0xFF43) & 0x0F); } }

		//FF44(LY)
		byte LY { 
			get { return mmu.Read(0xFF44); } 
			set { mmu.Write(0xFF44, value, true); } 
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
			this.mmu.OnMemoryWritten += (MMU m, ushort addr) => {
				if (addr >= 0x8000 && addr <= 0x97FF) {
					UpdateTile(addr);
				} else if (addr == 0xFF46) {
					OAMTransfer();
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
				if (clock >= VERTICAL_BLANK_CYCLES) {
					clock -= VERTICAL_BLANK_CYCLES;
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
			byte ly = LY;
			var lineY = ly + SCY;
			var lineX = SCX;
			var bufferY = (SCREEN_PIXELS_WIDTH * SCREEN_PIXELS_HEIGHT - (ly * SCREEN_PIXELS_WIDTH)) - SCREEN_PIXELS_WIDTH;

			if (LCDC_BGWindowDisplay) {
				var tileMapAddressOffset = LCDC_BGTileMap == 0 ? 0x9800 : 0x9C00;
				var tileMapX = 0;
				var tileMapY = 0;
				var tileX = 0;
				var tileY = 0;
				var nTile = 0;
				int[] tile = null;

				for (int i = 0; i < SCREEN_PIXELS_WIDTH; i++) {
					if (i == 0 || (lineX & 7) == 0) {
					
						tileMapY = (int)((lineY >> 3) & 31);
						tileMapX = (int)((lineX >> 3) & 31);

						nTile = mmu.Read((ushort)(tileMapAddressOffset + (tileMapY << 5) + tileMapX));
						if (LCDC_BGWindowTileData == 0) {
							if (nTile > 127) {
								nTile -= 0x100;
							}
							nTile = 256 + nTile;
						}

						if (!tiles.ContainsKey((uint)(nTile))) {
							continue;
						}
						tile = tiles[(uint)nTile];
					}

					tileY = (int)(lineY & 7);
					tileX = (int)(lineX & 7);

					buffer[bufferY + i] = colors[tile[(tileY << 3) + tileX]];
					lineX++;
				}
			}

			if (LCDC_SpriteDisplay) {
				var oamAddress = 0xFE00;
				int spriteX = 0;
				int spriteY = 0;
				byte n = 0;
				byte flags = 0;
				int palette = 0;
				bool xFlip = false;
				bool yFlip = false;
				int priority = 0;
				int spriteRow = 0;
				int pixelColor = 0;

				for (int i = 0; i < 40; i++) {
					spriteY = mmu.Read((ushort)(oamAddress + i * 4)) - 16;
					spriteX = mmu.Read((ushort)(oamAddress + i * 4 + 1)) - 8;
					n = mmu.Read((ushort)(oamAddress + i * 4 + 2));
					flags = mmu.Read((ushort)(oamAddress + i * 4 + 3));

					if (!tiles.ContainsKey((uint)n)) {
						continue;
					}

					if (spriteY <= ly && spriteY + 8 > ly) {
						
						palette = (flags & 0x10) == 0 ? 0 : 1;
						xFlip = (flags & 0x20) == 0 ? false : true;
						yFlip = (flags & 0x40) == 0 ? false : true;
						priority = (flags & 0x80) == 0 ? 0 : 1;

						//TODO: check y-flip

						spriteRow = (ly - spriteY);

						for (int x = 0; x < 8; x++) {
							pixelColor = tiles[(uint)n][spriteRow * 8 + x];
							if (((spriteX + x) >= 0) && ((spriteX + x) < SCREEN_PIXELS_WIDTH)
								&& pixelColor != 0
								//&& (priority != 0 || buffer[bufferY + spriteX + x] == colors[0])
							) {
								//TODO: check x-flip
								buffer[bufferY + spriteX + x] = colors[pixelColor];
							}
						}
					}

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

		void OAMTransfer()
		{
			var addr = (ushort)(DMA << 8);
			for (int i = 0; i < 40 * 4; i++) {
				mmu.Write((ushort)(0xFE00 + i), mmu.Read((ushort)(addr + i)));
			}
		}
	}
}
