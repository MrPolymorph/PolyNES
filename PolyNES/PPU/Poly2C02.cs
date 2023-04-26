using Microsoft.Xna.Framework;
using Poly6502.Microprocessor.Utilities;
using PolyNES.PPU.Data;
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
        private bool _enableNMI;
        
        #region Registers
        private readonly PpuControlRegister _controlRegister;
        private readonly PpuMaskRegister _maskRegister;
        private readonly PpuStatusRegister _statusRegister;
        private byte _oamAddress;
        private byte _oamData;
        private readonly PpuScrollRegister _scrollRegister;
        private byte _ppuAddress;
        private byte _ppuData;
        #endregion

        public Poly2C02(Cartridge.Cartridge cartridge)
        {
            FrameComplete = false;
            Screen = new Color[NesScreenWidth * NesScreenHeight];
            LeftPatternTableView = new Color[PatternTableSize];
            RightPatternTableView = new Color[PatternTableSize];
            _cartridge = cartridge;
            _cartridge.RegisterCartridgeLoadedCallback(CartridgeLoaded);
            _controlRegister = new PpuControlRegister();
            _maskRegister = new PpuMaskRegister();
            _statusRegister = new PpuStatusRegister();
            _scrollRegister = new PpuScrollRegister();
            _paletteTable = new byte[32];
            _patternTable = new[]
            {
                new byte[PatternTableSize],
                new byte[PatternTableSize]
            };
            
            _oddFrame = false;
            _enableNMI = false;
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
                        PpuWrite(vram_addr.reg, data);
                        // All writes from PPU data automatically increment the nametable
                        // address depending upon the mode set in the control register.
                        // If set to vertical mode, the increment is 32, so it skips
                        // one whole nametable row; in horizontal mode it just increments
                        // by 1, moving to the next column
                        vram_addr.reg += (control.increment_mode ? 32 : 1);
                        break;
                        return _ppuData;
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
        public override void Clock()
        {
            /* This is a dummy scanline, whose sole purpose is to fill the shift registers with the data for the first
               two tiles of the next scanline. Although no pixels are rendered for this scanline, the PPU still makes
               the same memory accesses it would for a regular scanline.
               
               This scanline varies in length, depending on whether an even or an odd frame is being rendered. For odd
               frames, the cycle at the end of the scanline is skipped (this is done internally by jumping directly
               from (339,261) to (0,0), replacing the idle tick at the beginning of the first visible scanline with the
               last tick of the last dummy nametable fetch). For even frames, the last cycle occurs normally. This is
               done to compensate for some shortcomings with the way the PPU physically outputs its video signal, the
               end result being a crisper image when the screen isn't scrolling. However, this behavior can be bypassed
               by keeping rendering disabled until after this scanline has passed, which results in an image that looks
               more like a traditionally interlaced picture.
               During pixels 280 through 304 of this scanline, the vertical scroll bits are reloaded if rendering is enabled.
             */ 

            //visible Scanlines
            if (_scanline >= -1 && _scanline < 240)
            { 
                // This is an idle cycle. The value on the PPU address bus during this cycle appears to be the same CHR
                // address that is later used to fetch the low background tile byte starting at dot 5 (possibly calculated
                // during the two unused NT fetches at the end of the previous scanline). 
                if (_scanline == -1 && _cycle == 1)
                {
                    _statusRegister.Reset();
                }

                // The data for each tile is fetched during this phase. Each memory access takes 2 PPU cycles to complete,
                // and 4 must be performed per tile:
                //     Nametable byte
                //     Attribute table byte
                //     Pattern table tile low
                //     Pattern table tile high (+8 bytes from pattern table tile low)
                //
                // The data fetched from these accesses is placed into internal latches, and then fed to the appropriate
                // shift registers when it's time to do so (every 8 cycles). Because the PPU can only fetch an attribute byte every 8 cycles,
                // each sequential string of 8 pixels is forced to have the same palette attribute.
                //
                // Sprite 0 hit acts as if the image starts at cycle 2 (which is the same cycle that the shifters shift for the first time),
                // so the sprite 0 flag will be raised at this point at the earliest. Actual pixel output is delayed further due to internal
                // render pipelining, and the first pixel is output during cycle 4.
                //
                // The shifters are reloaded during ticks 9, 17, 25, ..., 257.
                //
                // Note: At the beginning of each scanline, the data for the first two tiles is already loaded into the
                // shift registers (and ready to be rendered), so the first tile that gets fetched is Tile 3.
                //
                // While all of this is going on, sprite evaluation for the next scanline is taking place as a seperate process,
                // independent to what's happening here. 
                if (_cycle >= 1 && _cycle <= 256)
                {
                    
                }
            }

            // PostRender Scanline
            if (_scanline == 240)
            {
                
            }

            // Vertical blanking lines (241-260)
            // The VBlank flag of the PPU is set at tick 1 (the second tick) of scanline 241, where the VBlank NMI also
            // occurs. The PPU makes no memory accesses during these scanlines, so PPU memory can be freely accessed by
            // the program. 
            if (_scanline >= 241 && _scanline <= 260)
            {
                //Start VBlank if we have just entered the blanking phase
                if (_scanline == 241 && _cycle == 1)
                {
                    _statusRegister.SetFlag(PpuStatusRegisterFlags.V);

                    if (_controlRegister.HasFlag(PpuControlRegisterFlags.NonMaskableInterrupt))
                        _enableNMI = true;
                }
            }

            //advance to the next 'column'
            _cycle++;
            
            //end of cycles
            if (_cycle >= 341)
            {
                //reset cycles
                _cycle = 0;
                //advance to next 'row'
                _scanline++;
                
                //if the next row is then we reset and start again.
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
            _scrollRegister.SetFlag(0);
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