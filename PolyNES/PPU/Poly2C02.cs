using System;
using Microsoft.Xna.Framework;
using Poly6502.Microprocessor.Utilities;
using PolyNES.PPU.Data;
using PolyNES.PPU.Memory;
using PolyNES.PPU.Registers;

namespace PolyNES.PPU
{
    /// <summary>
    /// The PPU contains the following:
    ///
    /// Background:
    ///     VRAM address, temporary VRAM address, fine X scroll, and first/second write toggle -
    ///     This controls the addresses that the PPU reads during background rendering. See PPU scrolling.
    ///
    ///     2 16-bit shift registers - These contain the pattern table data for two tiles. Every 8 cycles,
    ///     the data for the next tile is loaded into the upper 8 bits of this shift register. Meanwhile, the pixel to
    ///     render is fetched from one of the lower 8 bits.
    ///
    ///     2 8-bit shift registers - These contain the palette attributes for the lower 8 pixels of the 16-bit shift register.
    ///     These registers are fed by a latch which contains the palette attribute for the next tile. Every 8 cycles,
    ///     the latch is loaded with the palette attribute for the next tile.
    ///
    ///  Sprites:
    ///     Primary OAM (holds 64 sprites for the frame)
    ///
    ///     Secondary OAM (holds 8 sprites for the current scanline)
    ///
    ///     8 pairs of 8-bit shift registers - These contain the pattern table data for up to 8 sprites, to be rendered
    ///     on the current scanline. Unused sprites are loaded with an all-transparent set of values.
    ///
    ///     8 latches - These contain the attribute bytes for up to 8 sprites.
    ///
    ///     8 counters - These contain the X positions for up to 8 sprites.
    /// </summary>
    public class Poly2C02 : AbstractAddressDataBus
    {
        public const int PatternTableSize = 0x1000;
        private const int MaxPPUCycles = 340;
        private const int ScanLineVisibleDots = 256;
        private const int NesScreenWidth = 256;
        private const int NesScreenHeight = 240;
        private const int PatternTableSizeInPixels = 128;
        private const int PatternTableRowSize = 256;
        private const int NesTilePixelSize = 8;
        private const int NesTileSize = 16;
        private const int NesPaletteStartAddress = 0x3F00;

        public readonly Color[] Screen;
        public readonly Color[] LeftPatternTableView;
        public readonly Color[] RightPatternTableView;
        public bool FrameComplete;
        
        private readonly Cartridge.Cartridge _cartridge;
        private readonly byte[] _paletteTable;
        private readonly byte[][] _patternTable;
        private int _scanline;
        private int _cycle;
        private bool _oddFrame;
        private bool _enableNonMaskableInterrupt;
        private bool _nonMaskableInterrupt;
        private byte[] _spriteShifterPatternLo;
        private byte[] _spriteShifterPatternHi;
        private byte _backgroundShifterPatternLo;
        private byte _backgroundShifterPatternHi;
        private byte _backgroundShifterAttributeLo;
        private byte _backgroundShifterAttributeHi;
        private ObjectAttribute[] _spriteScanLine;
        private ObjectAttribute[] _oam;
        private byte _nextBackgroundTileLo;
        private byte _nextBackgroundTileHi;
        private byte _nextBackgroundTileAttribute;
        private byte _spriteCount;
        private byte _nextBackgroundTileId;
        private byte _fineX;
        private bool _zeroHitPossible;
        private bool _renderSpriteZero;
        
        
        #region Registers
        private readonly PpuControlRegister _controlRegister;
        private readonly PpuMaskRegister _maskRegister;
        private readonly PpuStatusRegister _statusRegister;
        private byte _oamAddress;
        private byte _oamData;
        private readonly PpuScrollRegister _scrollRegister;
        private readonly PpuScrollRegister _transferScrollRegister;
        private byte _ppuAddress;
        private byte _ppuData;
        #endregion

        public Poly2C02(Cartridge.Cartridge cartridge)
        {
            FrameComplete = false;
            Screen = new Color[NesScreenWidth * NesScreenHeight];
            LeftPatternTableView = new Color[PatternTableSize];
            RightPatternTableView = new Color[PatternTableSize];
            _spriteScanLine = new ObjectAttribute[8];
            _oam = new ObjectAttribute[64];
            _cartridge = cartridge;
            _cartridge.RegisterCartridgeLoadedCallback(CartridgeLoaded);
            _controlRegister = new PpuControlRegister();
            _maskRegister = new PpuMaskRegister();
            _statusRegister = new PpuStatusRegister();
            _scrollRegister = new PpuScrollRegister();
            _transferScrollRegister = new PpuScrollRegister();
            _paletteTable = new byte[32];
            _patternTable = new[]
            {
                new byte[PatternTableSize],
                new byte[PatternTableSize]
            };
            
            _oddFrame = false;
            _enableNonMaskableInterrupt = false;
            _scanline = 0;
            _scanline = 0;
            
            //initialise Pattern Table
            for (int i = 0; i < PatternTableSize; i++)
            {
                LeftPatternTableView[i] = Color.White;
                RightPatternTableView[i] = Color.White;
            }
        }

        public override byte Read(ushort address, bool ronly = false)
        {
            SetPropagation(true);
            if (address >= 0x2000 && address <= 0x3EFF)
            {
                //check if its one of our registers
                var actualAddress = address & 0x2007;
                switch (actualAddress)
                {
                    case (0x2000): //Control Register
                    {
                        return _controlRegister.Register;
                    }
                    case (0x2001): //PPU Mask
                    {
                        return _maskRegister.Register;
                    }
                    case (0x2002): //PPU Status
                    {
                        //Apparently is you read the status register, it clears the VBLANK.
                        _statusRegister.SetFlag(PpuStatusRegisterFlags.V, false);
                        return _statusRegister.Register;
                    }
                    case (0x2003): //OAM Address
                    {
                        return _oamAddress;
                    }
                    case (0x2004): //OAM Data
                    {
                        return _oamData;
                    }
                    case (0x2005): //PPU Scroll
                    {
                        //Dunno what to do here yet.
                        break;
                    }
                    case (0x2006): //PPU Address
                    {
                        return _ppuAddress;
                    }
                    case (0x2007): //PPU Data
                    {
	                    return _ppuAddress;
                    }
                }
            }

            SetPropagation(false);
            return 0;
        }

        /// <summary>
        /// The PPU exposes eight memory-mapped registers to the CPU. These nominally sit at $2000 through $2007
        /// in the CPU's address space, but because they're incompletely decoded, they're mirrored in every 8 bytes
        /// from $2008 through $3FFF, so a write to $3456 is the same as a write to $2006. 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public override void Write(ushort address, byte data)
        {
            //Register Ranges
            if (address >= 0x2000 && address <= 0x3EFF)
            {
                //check if its one of our registers
                var actualAddress = address & 0x2007;
                switch (actualAddress)
                {
                    case (2000): //Control Register
                    {
                        _controlRegister.SetFlag(data);
                        break;
                    }
                    case (2001): //PPU Mask
                    {
                        _maskRegister.SetFlag(data);
                        break;
                    }
                    case (2002): //PPU Status
                    {
                        //You cannot write to the status register.
                        break;
                    }
                    case (2003): //OAM Address
                    {
                        _oamAddress = data;
                        break;
                    }
                    case (2004): //OAM Data
                    {
                        _oamData = data;
                        break;
                    }
                    case (2005): //PPU Scroll
                    {
                        if (_scrollRegister.Latched)
                        {

                        }

                        break;
                    }
                    case (2006): //PPU Address
                    {
                        break;
                    }
                    case (2007): //PPU Data
                    {
	                    PpuWrite(_scrollRegister.Register, data);
	                    // All writes from PPU data automatically increment the nametable
	                    // address depending upon the mode set in the control register.
	                    // If set to vertical mode, the increment is 32, so it skips
	                    // one whole nametable row; in horizontal mode it just increments
	                    // by 1, moving to the next column
	                    byte incrementAmount;
	                    incrementAmount = (byte) (_controlRegister.HasFlag(PpuControlRegisterFlags.PpuIncrement) ? 32 : 1);
	                    
	                    _scrollRegister.Register += incrementAmount;
	                    break;
                    }
                }
            }
        }

        private void PpuWrite(ushort address, byte data)
        {
            address &= 0x3FFF;
            
            //Pattern table range
            if (address >= 0 && address <= 0x1FFF)
            {
                var patternTableIndex = (address & PatternTableSize) >> 12; //left or right hand pattern table from the msb;
                var patternTableOffset = address & 0x0FFF; //read the rest of the data from the address to get the offset
                _patternTable[patternTableIndex][patternTableOffset] = data;
            }
            
            //Palette memory address range
            if (address >= 0x3F00 && address <= 0x3FFF)
            {
                address &= 0x001F;
                if (address == 0x0010) address = 0x0000;
                if (address == 0x0014) address = 0x0004;
                if (address == 0x0018) address = 0x0008;
                if (address == 0x001C) address = 0x000C;
                _paletteTable[address] = data;
            }
        }

        private byte PpuRead(ushort address)
        {
            //Pattern table range
            if (address >= 0 && address <= 0x1FFF)
            {
                var patternTableIndex = (address & PatternTableSize) >> 12; //left or right hand pattern table from the msb;
                var patternTableOffset = address & 0x0FFF; //read the rest of the data from the address to get the offset
                return _patternTable[patternTableIndex][patternTableOffset];
            }
            //Palette memory address range
            if (address >= 0x3F00 && address <= 0x3FFF)
            {
                address &= 0x001F;
                if (address == 0x0010) address = 0x0000;
                if (address == 0x0014) address = 0x0004;
                if (address == 0x0018) address = 0x0008;
                if (address == 0x001C) address = 0x000C;
                return (byte) (_paletteTable[address] & 
                               (_maskRegister.HasFlag(PpuMaskRegisterFlags.Greyscale) ? 0x30 : 0x3F));
            }

            return 0;
        }

        /// <summary>
        /// Every cycle, a bit is fetched from the 4 background shift registers in order to create a pixel on screen.
        /// Exactly which bit is fetched depends on the fine X scroll, set by $2005 (this is how fine X scrolling is possible).
        /// Afterwards, the shift registers are shifted once, to the data for the next pixel.
        ///
        /// Every 8 cycles/shifts, new data is loaded into these registers.
        ///
        /// Every cycle, the 8 x-position counters for the sprites are decremented by one. For each sprite,
        /// if the counter is still nonzero, nothing else happens. If the counter is zero, the sprite becomes "active",
        /// and the respective pair of shift registers for the sprite is shifted once every cycle. This output accompanies
        /// the data in the sprite's latch, to form a pixel. The current pixel for each "active" sprite is checked
        /// (from highest to lowest priority), and the first non-transparent pixel moves on to a multiplexer, where it
        /// joins the BG pixel.
        ///
        /// If the sprite has foreground priority or the BG pixel is zero, the sprite pixel is output.
        ///
        /// If the sprite has background priority and the BG pixel is nonzero, the BG pixel is output.
        /// (Note: Even though the sprite is "behind the background", it was still the the highest priority sprite to
        /// have a non-transparent pixel, and thus the only sprite to be looked at. Therefore, the BG pixel is output
        /// even if another foreground priority sprite is present at this pixel. This is where the sprite priority quirk
        /// comes from.)
        ///
        /// The PPU renders 262 scanlines per frame. Each scanline lasts for 341 PPU clock cycles
        /// (113.667 CPU clock cycles; 1 CPU cycle = 3 PPU cycles), with each clock cycle producing one pixel.
        /// The line numbers given here correspond to how the internal PPU frame counters count lines.
        ///
        /// The information in this section is summarized in the diagram in the next section.
        ///
        /// The timing below is for NTSC PPUs. PPUs for 50 Hz TV systems differ:
        /// Dendy PPUs render 51 post-render scanlines instead of 1
        /// PAL NES PPUs render 70 vblank scanlines instead of 20, and they additionally run 3.2 PPU cycles per CPU cycle,
        /// or 106.5625 CPU clock cycles per scanline.
        /// </summary>
        // public override void Clock()
        // {
        //     /* This is a dummy scanline, whose sole purpose is to fill the shift registers with the data for the first
        //        two tiles of the next scanline. Although no pixels are rendered for this scanline, the PPU still makes
        //        the same memory accesses it would for a regular scanline.
        //        
        //        This scanline varies in length, depending on whether an even or an odd frame is being rendered. For odd
        //        frames, the cycle at the end of the scanline is skipped (this is done internally by jumping directly
        //        from (339,261) to (0,0), replacing the idle tick at the beginning of the first visible scanline with the
        //        last tick of the last dummy nametable fetch). For even frames, the last cycle occurs normally. This is
        //        done to compensate for some shortcomings with the way the PPU physically outputs its video signal, the
        //        end result being a crisper image when the screen isn't scrolling. However, this behavior can be bypassed
        //        by keeping rendering disabled until after this scanline has passed, which results in an image that looks
        //        more like a traditionally interlaced picture.
        //        During pixels 280 through 304 of this scanline, the vertical scroll bits are reloaded if rendering is enabled.
        //      */ 
        //
        //     //visible Scanlines
        //     if (_scanline >= -1 && _scanline < 240)
        //     { 
        //         // This is an idle cycle. The value on the PPU address bus during this cycle appears to be the same CHR
        //         // address that is later used to fetch the low background tile byte starting at dot 5 (possibly calculated
        //         // during the two unused NT fetches at the end of the previous scanline). 
        //         if (_scanline == -1 && _cycle == 1)
        //         {
        //             _statusRegister.Reset();
        //         }
        //
        //         // The data for each tile is fetched during this phase. Each memory access takes 2 PPU cycles to complete,
        //         // and 4 must be performed per tile:
        //         //     Nametable byte
        //         //     Attribute table byte
        //         //     Pattern table tile low
        //         //     Pattern table tile high (+8 bytes from pattern table tile low)
        //         //
        //         // The data fetched from these accesses is placed into internal latches, and then fed to the appropriate
        //         // shift registers when it's time to do so (every 8 cycles). Because the PPU can only fetch an attribute byte every 8 cycles,
        //         // each sequential string of 8 pixels is forced to have the same palette attribute.
        //         //
        //         // Sprite 0 hit acts as if the image starts at cycle 2 (which is the same cycle that the shifters shift for the first time),
        //         // so the sprite 0 flag will be raised at this point at the earliest. Actual pixel output is delayed further due to internal
        //         // render pipelining, and the first pixel is output during cycle 4.
        //         //
        //         // The shifters are reloaded during ticks 9, 17, 25, ..., 257.
        //         //
        //         // Note: At the beginning of each scanline, the data for the first two tiles is already loaded into the
        //         // shift registers (and ready to be rendered), so the first tile that gets fetched is Tile 3.
        //         //
        //         // While all of this is going on, sprite evaluation for the next scanline is taking place as a seperate process,
        //         // independent to what's happening here. 
        //         if (_cycle >= 1 && _cycle <= 256)
        //         {
        //             
        //         }
        //     }
        //
        //     // PostRender Scanline
        //     if (_scanline == 240)
        //     {
        //         
        //     }
        //
        //     // Vertical blanking lines (241-260)
        //     // The VBlank flag of the PPU is set at tick 1 (the second tick) of scanline 241, where the VBlank NMI also
        //     // occurs. The PPU makes no memory accesses during these scanlines, so PPU memory can be freely accessed by
        //     // the program. 
        //     if (_scanline >= 241 && _scanline <= 260)
        //     {
        //         //Start VBlank if we have just entered the blanking phase
        //         if (_scanline == 241 && _cycle == 1)
        //         {
        //             _statusRegister.SetFlag(PpuStatusRegisterFlags.V);
        //
        //             if (_controlRegister.HasFlag(PpuControlRegisterFlags.NonMaskableInterrupt))
        //                 _enableNMI = true;
        //         }
        //     }
        //
        //     //advance to the next 'column'
        //     _cycle++;
        //     
        //     //end of cycles
        //     if (_cycle >= 341)
        //     {
        //         //reset cycles
        //         _cycle = 0;
        //         //advance to next 'row'
        //         _scanline++;
        //         
        //         //if the next row is then we reset and start again.
        //         if (_scanline >= 261)
        //         {
        //             _scanline = -1;
        //             FrameComplete = true;
        //             _oddFrame = !_oddFrame;
        //         }
        //     }
        // }
        
        private void Tax()
        {
	        if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds) || 
	            _maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites))
	        {
		        _scrollRegister.NametableX = _transferScrollRegister.NametableX;
		        _scrollRegister.CoarseX = _transferScrollRegister.CoarseX;
	        }
        }
        
        private void  IncrementScrollX()
        {
	        if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds) || 
	            _maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites))
	        {
		        if (_scrollRegister.CoarseX == 31)
		        {
			        // Leaving nametable so wrap address round
			        _scrollRegister.CoarseX = 0;
			        // Flip target nametable bit
			        _scrollRegister.NametableX = (byte) ~_scrollRegister.NametableX;
		        }
		        else
		        {
			        // Staying in current nametable, so just increment
			        _scrollRegister.CoarseX++;
		        }
	        }
        }
        
		private void IncrementScrollY()
		{
			if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds) || 
			    _maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites))
			{
				// If possible, just increment the fine y offset
				if (_scrollRegister.FineY < 7)
				{
					_scrollRegister.FineY++;
				}
				else
				{
					_scrollRegister.FineY = 0;
					
					if (_scrollRegister.CoarseY == 29)
					{
						_scrollRegister.CoarseY = 0;
						_scrollRegister.NametableY = (byte) ~_scrollRegister.NametableY;
					}
					else if (_scrollRegister.CoarseY == 31)
					{
						_scrollRegister.CoarseY = 0;
					}
					else
					{
						_scrollRegister.CoarseY++;
					}
				}
			}
		}
		
		private void Tay()
		{
			if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds) || 
			    _maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites))
			{
				_scrollRegister.FineY = _transferScrollRegister.FineY;
				_scrollRegister.NametableY = _transferScrollRegister.NametableY;
				_scrollRegister.CoarseY = _transferScrollRegister.CoarseY;
			}
		}
		
		// ==============================================================================
		// Prime the "in-effect" background tile shifters ready for outputting next
		// 8 pixels in scanline.
		private void LoadBackgroundShifters()
		{
			_backgroundShifterPatternLo = (byte) ((_backgroundShifterPatternLo & 0xFF00) | _nextBackgroundTileLo);
			_backgroundShifterPatternHi = (byte) ((_backgroundShifterPatternHi & 0xFF00) | _nextBackgroundTileHi);
			
			_backgroundShifterAttributeLo  = (byte) ((_backgroundShifterPatternLo & 0xFF00) | ((_nextBackgroundTileAttribute & 0b01) != 0 ? 0xFF : 0x00));
			_backgroundShifterAttributeHi  = (byte) ((_backgroundShifterPatternHi & 0xFF00) | ((_nextBackgroundTileAttribute & 0b10) != 0 ? 0xFF : 0x00));
		}
		
		private void UpdateShifters()
		{
			if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds))
			{
				// Shifting background tile pattern row
				_backgroundShifterPatternLo <<= 1;
				_backgroundShifterPatternHi <<= 1;

				// Shifting palette attributes by 1
				_backgroundShifterAttributeLo <<= 1;
				_backgroundShifterAttributeHi <<= 1;
			}

			if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites) && _cycle >= 1 && _cycle < 258)
			{
				for (int i = 0; i < _spriteCount; i++)
				{
					if (_spriteScanLine[i].X > 0)
					{
						_spriteScanLine[i].X--;
					}
					else
					{
						_spriteShifterPatternLo[i] <<= 1;
						_spriteShifterPatternHi[i] <<= 1;
					}
				}
			}
		}
        
		public override void Clock()
		{

			// All but 1 of the secanlines is visible to the user. The pre-render scanline
			// at -1, is used to configure the "shifters" for the first visible scanline, 0.
			if (_scanline >= -1 && _scanline < 240)
			{		
				// Background Rendering ======================================================

				if (_scanline == 0 && _cycle == 0 && _oddFrame && 
				    (_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds) || 
				     _maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites)))
				{
					// "Odd Frame" cycle skip
					_cycle = 1;
				}

				if (_scanline == -1 && _cycle == 1)
				{
					// Effectively start of new frame, so clear vertical blank flag
					_statusRegister.SetFlag(PpuStatusRegisterFlags.V, false);
					_statusRegister.SetFlag(PpuStatusRegisterFlags.O, false);
					_statusRegister.SetFlag(PpuStatusRegisterFlags.S, false);
					
					// Clear Shifters
					for (int i = 0; i < 8; i++)
					{
						_spriteShifterPatternLo[i] = 0;
						_spriteShifterPatternHi[i] = 0;
					}
				}


				if ((_cycle >= 2 && _cycle < 258) || (_cycle >= 321 && _cycle < 338))
				{
					UpdateShifters();
					
					switch ((_cycle - 1) % 8)
					{
						case 0:
							LoadBackgroundShifters();
							
							_nextBackgroundTileId = PpuRead((ushort)(0x2000 | (_scrollRegister.Register & 0x0FFF)));
							
							break;
						case 2:
							_nextBackgroundTileAttribute = PpuRead((ushort)(0x23C0 | (_scrollRegister.NametableY << 11) 
							                                     | (_scrollRegister.NametableX << 10) 
							                                     | ((_scrollRegister.CoarseY >> 2) << 3) 
							                                     | (_scrollRegister.CoarseX >> 2)));
							
							if ((_scrollRegister.CoarseY & 0x02) != 0) _nextBackgroundTileAttribute >>= 4;
							if ((_scrollRegister.CoarseX & 0x02) != 0) _nextBackgroundTileAttribute >>= 2;
							_nextBackgroundTileAttribute &= 0x03;
							break;
						case 4:
							var backgroundPatternTableAddress = 0;
							backgroundPatternTableAddress = _controlRegister.HasFlag(PpuControlRegisterFlags.BackgroundPatternTableAddress) ? 0x1000 : 0;
							_nextBackgroundTileLo = PpuRead((ushort)((backgroundPatternTableAddress << 12) 
							                           + (_nextBackgroundTileId << 4) 
							                           + (_scrollRegister.FineY)));

						break;
					case 6:
						backgroundPatternTableAddress = _controlRegister.HasFlag(PpuControlRegisterFlags.BackgroundPatternTableAddress) ? 0x1000 : 0;
						_nextBackgroundTileHi = PpuRead((ushort)((backgroundPatternTableAddress << 12)
						                           + (_nextBackgroundTileId << 4)
						                           + (_scrollRegister.FineY) + 8));
						break;
					case 7:
						IncrementScrollX();
						break;
					}
				}

				// End of a visible scanline, so increment downwards...
				if (_cycle == 256)
				{
					IncrementScrollY();
				}

				//...and reset the x position
				if (_cycle == 257)
				{
					LoadBackgroundShifters();
					Tax();
				}

				// Superfluous reads of tile id at end of scanline
				if (_cycle == 338 || _cycle == 340)
				{
					_nextBackgroundTileId = PpuRead((ushort)(0x2000 | (_scrollRegister.Register & 0x0FFF)));
				}

				if (_scanline == -1 && _cycle >= 280 && _cycle < 305)
				{
					// End of vertical blank period so reset the Y address ready for rendering
					Tay();
				}
				
				if (_cycle == 257 && _scanline >= 0)
				{
					foreach (var ssl in _spriteScanLine)
					{
						ssl.Reset();
					}

					_spriteCount = 0;

					for (byte i = 0; i < 8; i++)
					{
						_spriteShifterPatternLo[i] = 0;
						_spriteShifterPatternHi[i] = 0;
					}
					
					byte oamEntry = 0;
					
					_zeroHitPossible = false;

					while (oamEntry < 64 && _spriteCount < 9)
					{
						ushort diff = (ushort)(_scanline - _oam[oamEntry].Y);

						var spriteSize = 0;

						spriteSize = _controlRegister.HasFlag(PpuControlRegisterFlags.SpriteSize) ? 16 : 8;
						
						if (diff >= 0 && diff < spriteSize && _spriteCount < 8)
						{
							if (_spriteCount < 8)
							{
								if (oamEntry == 0)
								{
									_zeroHitPossible = true;
								}

								_spriteScanLine[_spriteCount].Set(_oam[oamEntry]);
							}			
							_spriteCount++;
						}
						oamEntry++;
					} // End of sprite evaluation for next scanline

					// Set sprite overflow flag
					_statusRegister.SetFlag(PpuStatusRegisterFlags.O, _spriteCount >= 8);
				}

				if (_cycle == 340)
				{
					for (byte i = 0; i < _spriteCount; i++)
					{
						byte sprite_pattern_bits_lo, sprite_pattern_bits_hi;
						ushort SpritePatternTableAddressLo, sprite_pattern_addr_hi;
						
						if (!_controlRegister.HasFlag(PpuControlRegisterFlags.SpriteSize))
						{
							var patternTable = 0;
							patternTable = _controlRegister.HasFlag(PpuControlRegisterFlags.SpritePatternTableAddress) ? 0x1000 : 0;
							
							if ((_spriteScanLine[i].Attribute & 0x80) == 0)
							{
								SpritePatternTableAddressLo = 
								  (ushort) ((patternTable)  // Which Pattern Table? 0KB or 4KB offset
								            | (_spriteScanLine[i].Id   << 4   )  // Which Cell? Tile ID * 16 (16 bytes per tile)
								            | (_scanline - _spriteScanLine[i].Y)); // Which Row in cell? (0->7)
														
							}
							else
							{
								SpritePatternTableAddressLo = 
								  (ushort) ((patternTable)  // Which Pattern Table? 0KB or 4KB offset
								            | (_spriteScanLine[i].Id   << 4   )  // Which Cell? Tile ID * 16 (16 bytes per tile)
								            | (7 - (_scanline - _spriteScanLine[i].Y))); // Which Row in cell? (7->0)
							}

						}
						else
						{
							// 8x16 Sprite Mode - The sprite attribute determines the pattern table
							if ((_spriteScanLine[i].Attribute & 0x80) == 0)
							{
								// Sprite is NOT flipped vertically, i.e. normal
								if (_scanline - _spriteScanLine[i].Y < 8)
								{
									// Reading Top half Tile
									SpritePatternTableAddressLo = 
									  (ushort) (((_spriteScanLine[i].Id & 0x01)      << 12)  // Which Pattern Table? 0KB or 4KB offset
									            | ((_spriteScanLine[i].Id & 0xFE)      << 4 )  // Which Cell? Tile ID * 16 (16 bytes per tile)
									            | ((_scanline - _spriteScanLine[i].Y) & 0x07 )); // Which Row in cell? (0->7)
								}
								else
								{
									// Reading Bottom Half Tile
									SpritePatternTableAddressLo = 
									  (ushort) (( (_spriteScanLine[i].Id & 0x01)      << 12)  // Which Pattern Table? 0KB or 4KB offset
									            | (((_spriteScanLine[i].Id & 0xFE) + 1) << 4 )  // Which Cell? Tile ID * 16 (16 bytes per tile)
									            | ((_scanline - _spriteScanLine[i].Y) & 0x07  )); // Which Row in cell? (0->7)
								}
							}
							else
							{
								// Sprite is flipped vertically, i.e. upside down
								if (_scanline - _spriteScanLine[i].Y < 8)
								{
									// Reading Top half Tile
									SpritePatternTableAddressLo = 
									  (ushort) (( (_spriteScanLine[i].Id & 0x01)      << 12)    // Which Pattern Table? 0KB or 4KB offset
									            | (((_spriteScanLine[i].Id & 0xFE) + 1) << 4 )    // Which Cell? Tile ID * 16 (16 bytes per tile)
									            | (7 - (_scanline - _spriteScanLine[i].Y) & 0x07)); // Which Row in cell? (0->7)
								}
								else
								{
									// Reading Bottom Half Tile
									SpritePatternTableAddressLo = 
									  (ushort) (((_spriteScanLine[i].Id & 0x01)       << 12)    // Which Pattern Table? 0KB or 4KB offset
									            | ((_spriteScanLine[i].Id & 0xFE)       << 4 )    // Which Cell? Tile ID * 16 (16 bytes per tile)
									            | (7 - (_scanline - _spriteScanLine[i].Y) & 0x07)); // Which Row in cell? (0->7)
								}
							}
						}

						// Phew... XD I'm absolutely certain you can use some fantastic bit 
						// manipulation to reduce all of that to a few one liners, but in this
						// form it's easy to see the processes required for the different
						// sizes and vertical orientations

						// Hi bit plane equivalent is always offset by 8 bytes from lo bit plane
						sprite_pattern_addr_hi = (ushort) (SpritePatternTableAddressLo + 8);

						// Now we have the address of the sprite patterns, we can read them
						sprite_pattern_bits_lo = PpuRead(SpritePatternTableAddressLo);
						sprite_pattern_bits_hi = PpuRead(sprite_pattern_addr_hi);

						// If the sprite is flipped horizontally, we need to flip the 
						// pattern bytes. 
						if ((_spriteScanLine[i].Attribute & 0x40) == 1)
						{
							// This little lambda function "flips" a byte
							// so 0b11100000 becomes 0b00000111. It's very
							// clever, and stolen completely from here:
							// https://stackoverflow.com/a/2602885
							Func<byte, byte> flipByte = (b) =>
							{
								b = (byte) ((b & 0xF0) >> 4 | (b & 0x0F) << 4);
								b = (byte) ((b & 0xCC) >> 2 | (b & 0x33) << 2);
								b = (byte) ((b & 0xAA) >> 1 | (b & 0x55) << 1);
								return b;
							};

							// Flip Patterns Horizontally
							sprite_pattern_bits_lo = flipByte(sprite_pattern_bits_lo);
							sprite_pattern_bits_hi = flipByte(sprite_pattern_bits_hi);
						}

						// Finally! We can load the pattern into our sprite shift registers
						// ready for rendering on the next scanline
						_spriteShifterPatternLo[i] = sprite_pattern_bits_lo;
						_spriteShifterPatternHi[i] = sprite_pattern_bits_hi;
					}
				}
			}

			if (_scanline == 240)
			{
				// Post Render Scanline - Do Nothing!
			}

			if (_scanline >= 241 && _scanline < 261)
			{
				if (_scanline == 241 && _cycle == 1)
				{
					// Effectively end of frame, so set vertical blank flag
					_statusRegister.SetFlag(PpuStatusRegisterFlags.V, true);

					// If the control register tells us to emit a NMI when
					// entering vertical blanking period, do it! The CPU
					// will be informed that rendering is complete so it can
					// perform operations with the PPU knowing it wont
					// produce visible artefacts
					if (_controlRegister.HasFlag(PpuControlRegisterFlags.NonMaskableInterrupt)) 
						_nonMaskableInterrupt = true;
				}
			}
			
			// Composition - We now have background & foreground pixel information for this cycle

			// Background =============================================================
			byte bg_pixel = 0x00;   // The 2-bit pixel to be rendered
			byte backroundPalette = 0x00; // The 3-bit index of the palette the pixel indexes

			// We only render backgrounds if the PPU is enabled to do so. Note if 
			// background rendering is disabled, the pixel and palette combine
			// to form 0x00. This will fall through the colour tables to yield
			// the current background colour in effect
			if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds))
			{
				if (_maskRegister.HasFlag(PpuMaskRegisterFlags.LeftBackground) || (_cycle >= 9))
				{
					// Handle Pixel Selection by selecting the relevant bit
					// depending upon fine x scolling. This has the effect of
					// offsetting ALL background rendering by a set number
					// of pixels, permitting smooth scrolling
					ushort bitMultiplex = (ushort) (0x8000 >> _fineX);

					// Select Plane pixels by extracting from the shifter 
					// at the required location. 
					byte pixel0 = (byte) ((_backgroundShifterPatternLo & bitMultiplex) > 0 ? 1 : 0);
					byte pixel1 = (byte) ((_backgroundShifterPatternHi & bitMultiplex) > 0 ? 1 : 0);

					// Combine to form pixel index
					bg_pixel = (byte) ((pixel1 << 1) | pixel0);

					// Get palette
					byte backgroundPalette0 = (byte) ((_backgroundShifterAttributeLo & bitMultiplex) > 0 ? 1 : 0);
					byte backroundPalette1 = (byte) ((_backgroundShifterAttributeHi & bitMultiplex) > 0 ? 1 : 0);
					backroundPalette = (byte) ((backroundPalette1 << 1) | backgroundPalette0);
				}
			}

			// Foreground =============================================================
			byte foregroundPixel = 0x00;   // The 2-bit pixel to be rendered
			byte foregroundPalette = 0x00; // The 3-bit index of the palette the pixel indexes
			bool foregroundPriority = false; // A bit of the sprite attribute indicates if its
									          // more important than the background
			if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites))
			{
				// Iterate through all sprites for this scanline. This is to maintain
				// sprite priority. As soon as we find a non transparent pixel of
				// a sprite we can abort
				if (_maskRegister.HasFlag(PpuMaskRegisterFlags.LeftSprites) || (_cycle >= 9))
				{
					_renderSpriteZero = false;

					for (byte i = 0; i < _spriteCount; i++)
					{
						// Scanline cycle has "collided" with sprite, shifters taking over
						if (_spriteScanLine[i].X == 0)
						{
							// Note Fine X scrolling does not apply to sprites, the game
							// should maintain their relationship with the background. So
							// we'll just use the MSB of the shifter

							// Determine the pixel value...
							
							byte foregroundPixelLo = (byte) ((_spriteShifterPatternLo[i] & 0x80) > 0 ? 1 : 0);
							byte foregroundPixelHi = (byte) ((_spriteShifterPatternHi[i] & 0x80) > 0 ? 1 : 0);
							foregroundPixel = (byte) ((foregroundPixelHi << 1) | foregroundPixelLo);

							// Extract the palette from the bottom two bits. Recall
							// that foreground palettes are the latter 4 in the 
							// palette memory.
							foregroundPalette = (byte) ((_spriteScanLine[i].Attribute & 0x03) + 0x04);
							foregroundPriority = (_spriteScanLine[i].Attribute & 0x20) == 0;

							// If pixel is not transparent, we render it, and dont
							// bother checking the rest because the earlier sprites
							// in the list are higher priority
							if (foregroundPixel != 0)
							{
								if (i == 0) // Is this sprite zero?
								{
									_renderSpriteZero = true;
								}

								break;
							}
						}
					}
				}		
			}
			
			byte pixel = 0x00;   // The FINAL Pixel...
			byte palette = 0x00; // The FINAL Palette...

			if (bg_pixel == 0 && foregroundPixel == 0)
			{
				// The background pixel is transparent
				// The foreground pixel is transparent
				// No winner, draw "background" colour
				pixel = 0x00;
				palette = 0x00;
			}
			else if (bg_pixel == 0 && foregroundPixel > 0)
			{
				// The background pixel is transparent
				// The foreground pixel is visible
				// Foreground wins!
				pixel = foregroundPixel;
				palette = foregroundPalette;
			}
			else if (bg_pixel > 0 && foregroundPixel == 0)
			{
				// The background pixel is visible
				// The foreground pixel is transparent
				// Background wins!
				pixel = bg_pixel;
				palette = backroundPalette;
			}
			else if (bg_pixel > 0 && foregroundPixel > 0)
			{
				// The background pixel is visible
				// The foreground pixel is visible
				// Hmmm...
				if (foregroundPriority)
				{
					// Foreground cheats its way to victory!
					pixel = foregroundPixel;
					palette = foregroundPalette;
				}
				else
				{
					// Background is considered more important!
					pixel = bg_pixel;
					palette = backroundPalette;
				}

				// Sprite Zero Hit detection
				if (_zeroHitPossible && _renderSpriteZero)
				{
					// Sprite zero is a collision between foreground and background
					// so they must both be enabled
					if (_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds) &
					    _maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites))
					{
						// The left edge of the screen has specific switches to control
						// its appearance. This is used to smooth inconsistencies when
						// scrolling (since sprites x coord must be >= 0)
						if (!(_maskRegister.HasFlag(PpuMaskRegisterFlags.LeftBackground) | 
						      _maskRegister.HasFlag(PpuMaskRegisterFlags.LeftSprites)))
						{
							if (_cycle >= 9 && _cycle < 258)
							{
								_statusRegister.SetFlag(PpuStatusRegisterFlags.S);
							}
						}
						else
						{
							if (_cycle >= 1 && _cycle < 258)
							{
								_statusRegister.SetFlag(PpuStatusRegisterFlags.S);
							}
						}
					}
				}
			}

			// Now we have a final pixel colour, and a palette for this cycle
			// of the current scanline. Let's at long last, draw that ^&%*er :P
			int x = _cycle - 1;
			int y = _scanline;
			Screen[NesScreenWidth + x * y] = GetPaletteColor(palette, pixel);
			
			_cycle++;
			
			if(_maskRegister.HasFlag(PpuMaskRegisterFlags.Backgrounds) || 
			   _maskRegister.HasFlag(PpuMaskRegisterFlags.Sprites))
				if (_cycle == 260 && _scanline < 240)
				{
					//cart->GetMapper()->scanline();
				}

			if (_cycle >= 341)
			{
				_cycle = 0;
				_scanline++;
				if (_scanline >= 261)
				{
					_scanline = -1;
					FrameComplete = true;
					_oddFrame = !_oddFrame;
				}
			}
		}

        public void DrawPatternTable(int patternTableSelector, byte palleteId)
        {
            for (int tileX = 0; tileX < NesTileSize; tileX++)
            {
                for (int tileY = 0; tileY < NesTileSize; tileY++)
                {
                    // each tile is 8x8 pixels at 2bits per pixel = 16.
                    // for any given row in the pattern table there are 16 of these tiles 
                    // that gives us 16 bytes by 16 tiles = 256
                    // all this together gives us our index 
                    var patternTableOffset = tileY * PatternTableRowSize + tileX * NesTileSize;
                    
                    //now for each tile we have 8 rows with 8 pixels (64 pixels in total)
                    //lets go round them for this particular tile
                    for (int pixelRow = 0; pixelRow < NesTilePixelSize; pixelRow++)
                    {
                        //get the least significant byte plane for the tile.
                        //this address formula is basically: left table or right table * the size of that table (4KB)
                        //The offset into that table + the current byte row  ;
                        ushort address = (ushort)(patternTableSelector * PatternTableSize + patternTableOffset + pixelRow);
                        var loByte = PpuRead(address);
                        var hiByte = PpuRead((ushort)(address + 8));

                        for (int pixelColumn = 0; pixelColumn < NesTilePixelSize; pixelColumn++)
                        {
                            //we only want the lest significant bit from both planes
                            byte pixelId = (byte)((hiByte & 0x01) << 1 | (loByte & 0x01));
                            
                            //Shift each time we loop so that we read the least significant bit each time
                            loByte >>= 1;
                            hiByte >>= 1;

                            //we want to draw starting top left, so we invert our start index.
                            var patternTablePixelXPosition = tileX * 8 + (7 - pixelColumn);
                            var patternTablePixelYPosition = tileY * 8 + pixelRow;
                            
                            var patternTableIndex = patternTablePixelXPosition + patternTablePixelYPosition;
                            var pixelColour = GetPaletteColor(palleteId, pixelId);

                            if (patternTableSelector == 0)
                            {
                                LeftPatternTableView[patternTableIndex] = pixelColour;
                            }
                            else
                            {
                                RightPatternTableView[patternTableIndex] = pixelColour;
                            }

                        }
                    }
                }
            }
        }

        private Color GetPaletteColor(byte paletteId, byte pixelId)
        {
            ushort address = (ushort)(NesPaletteStartAddress + (paletteId << 4) + pixelId);
            byte paletteIndex = _cartridge.Read(address);

            return NesColor.Palette[paletteIndex];
        }

        public override void SetRW(bool rw)
        {

        }

        public void RES()
        {
            _controlRegister.SetFlag(0);
            _maskRegister.SetFlag(0);
            _statusRegister.Reset();
            _oamAddress = 0;
            _ppuAddress = 0;
            _ppuData = 0;
        }

        private void CartridgeLoaded()
        {
            // if (!_cartridge.Header.NesFlags6.Mirroring)
            // {
            //     _nametable0Address = _nametable1Address = 0;
            //     _nametable2Address = _nametable3Address = 0x400;
            // }
            // else
            // {
            //     _nametable0Address = _nametable1Address = 0;
            //     _nametable1Address = _nametable3Address = 0x400;
            // }
        }
    }
}