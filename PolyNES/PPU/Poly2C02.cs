using System;
using Microsoft.Xna.Framework;
using Poly6502.Microprocessor.Utilities;
using PolyNES.Cartridge.Interfaces;
using PolyNES.PPU.Enums;
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
        private const int MaxPPUCycles = 340;
        private const int ScanLineVisibleDots = 256;
     
        public readonly Color[] Screen;
        public bool FrameComplete;
        
        private readonly Cartridge.Cartridge _cartridge;
        private readonly Random _random;
        private RenderState _currentRenderState;
        private ushort _nametable0Address, _nametable1Address, _nametable2Address, _nametable3Address;
        private byte _horizontalLatch, _verticalLatch;
        private byte[] _paletteTable;
        private Color[] _nametable0;
        private Color[] _nametable1;
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
            _cartridge = cartridge;
            _cartridge.RegisterCartridgeLoadedCallback(CartridgeLoaded);
            _controlRegister = new PpuControlRegister();
            _maskRegister = new PpuMaskRegister();
            _statusRegister = new PpuStatusRegister();
            _scrollRegister = new PpuScrollRegister();
            _paletteTable = new byte[32];
            Screen = new Color[256 * 240];
            _currentRenderState = RenderState.PreRender;
            _oddFrame = false;
            _enableNMI = false;
            _scanline = 0;
            _scanline = 0;
            _horizontalLatch = 0;
            _verticalLatch = 0;
            _nametable0 = new Color[256 * 240];
            _nametable1 = new Color[256 * 240];
        }

        public override byte Read(ushort address, bool ronly = false)
        {
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
                        return _ppuData;
                    }
                }
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

        public void DrawPatternTable()
        {
            for (int i = 0; i < _cartridge.LeftPatternTable.Length; i++)
            {
                var tile = _cartridge.LeftPatternTable[i];
                var plane0 = tile & 0x0F;
                var plane1 = tile & 0xF0;
                var colorIndex = 1;
                
                //If neither bit is set to 1: The pixel is background/transparent.
                //If only the bit in the first plane is set to 1: The pixel's color index is 1.
                if (plane0 == 1 && plane1 == 0)
                    colorIndex = 1;
                //If only the bit in the second plane is set to 1: The pixel's color index is 2.
                if (plane0 == 0 && plane1 == 1)
                    colorIndex = 2;
                if (plane0 == 1 && plane1 == 1)
                    colorIndex = 3;
            }
            
            for (int r = 0; r < 960; r++) {
                for (int col = 0; col < 256; col++) {
                    var tile_id = ((r / 8) * 32) + (col / 8);												//	sequential tile number
                    var tile_nr = _cartridge.Read((ushort)(0x2000 + (r / 8 * 32) + (col / 8)));									//	tile ID at the current address
                    var backgroundPatternTableAddress = _controlRegister.HasFlag(PpuControlRegisterFlags.BackgroundPatternTableAddress) ? 0x1000 : 0x000;
                    ushort adr = (ushort)(backgroundPatternTableAddress + (tile_nr * 0x10) + (r % 8));	//address of the tile in CHR RAM

                    //	select the correct byte of the attribute table
                    var tile_attr_nr = Read((ushort)(((0x2000 + (r / 8 * 32) + (col / 8)) & 0xfc00) + 0x03c0 + ((r / 32) * 8) + (col / 32)));
                    //	select the part of the byte that we need (2-bits)
                    var attr_shift = (((tile_id % 32) / 2 % 2) + (tile_id / 64 % 2) * 2) * 2;
                    var palette_offset = ((tile_attr_nr >> attr_shift) & 0x3) * 4;
                    var pixel = ((Read(adr) >> (7 - (col % 8))) & 1) + (((Read((ushort)(adr + 8)) >> (7 - (col % 8))) & 1) * 2);
                    framebuffer[(r * 256 * 3) + (col * 3)] = (PALETTE[VRAM[0x3f00 + palette_offset + pixel]] >> 16) & 0xff;
                    framebuffer[(r * 256 * 3) + (col * 3) + 1] = (PALETTE[VRAM[0x3f00 + palette_offset + pixel]] >> 8) & 0xff;
                    framebuffer[(r * 256 * 3) + (col * 3) + 2] = (PALETTE[VRAM[0x3f00 + palette_offset + pixel]]) & 0xff;

                }
            }

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
            if (!_cartridge.Header.NesFlags6.Mirroring)
            {
                _nametable0Address = _nametable1Address = 0;
                _nametable2Address = _nametable3Address = 0x400;
            }
            else
            {
                _nametable0Address = _nametable1Address = 0;
                _nametable1Address = _nametable3Address = 0x400;
            }
        }
    }
}