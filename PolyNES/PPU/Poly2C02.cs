using System;
using Microsoft.Xna.Framework;
using Poly6502.Microprocessor.Utilities;
using PolyNES.Cartridge.Interfaces;
using PolyNES.Extensions;
using PolyNES.PPU.Data;
using PolyNES.PPU.Enums;
using PolyNES.PPU.Memory;
using PolyNES.PPU.Registers;

namespace PolyNES.PPU
{
    /// <summary>
    /// PPU Memory Map
    ///          |______________________________________|
    /// 0xFFFF - |             Mirrors                  |
    ///          |         0x0000 -> 0x3FFF             |
    ///          |______________________________________|
    /// 0x4000 - |                                      |
    ///          |              Palettes                |
    /// 0x3F00 - |______________________________________|
    ///          |                                      |
    ///          |             Name Tables              |
    ///          |               (VRAM)                 |
    /// 0x2000 - |______________________________________|
    ///          |                                      |
    ///          |            Pattern Tables            |
    ///          |              (CHR ROM)               |
    ///  0x0000 -|--------------------------------------|
    /// </summary>
    public class Poly2C02 : AbstractAddressDataBus
    {
        private const int MaxPPUCycles = 340;
        private const int ScanLineVisibleDots = 256;
     
        public readonly Color[] Screen;
        
        private readonly ICartridge _cartridge;
        private readonly Random _random;
        /// <summary>
        /// This Video ram is the external 2KB ram chip
        /// which contains the 2 nametables.
        /// </summary>
        private readonly VideoRam _videoRam;
        private byte[] _paletteTable;
        private ushort _nametable0, _nametable1, _nametable2, _nametable3;
        //scanline can be through of as the current column
        private int _currentScanline;
        //cycle can be thought of as the current row
        private int _currentCycle;
        private RenderState _currentRenderState;
        public bool FrameComplete;
        private bool _showBackground;
        private bool _showSprites;
        private int _frame;
        private ushort _temporaryAddressStore;
        private int _fineX;
        private bool _hideEdgeBackground;
        
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

        public Poly2C02(ICartridge cartridge)
        {
            _cartridge = cartridge;
            _cartridge.RegisterCartridgeLoadedCallback(CartridgeLoaded);
            _controlRegister = new PpuControlRegister();
            _maskRegister = new PpuMaskRegister();
            _statusRegister = new PpuStatusRegister();
            _scrollRegister = new PpuScrollRegister();
            _videoRam = new VideoRam();
            _currentScanline = 0;
            _currentCycle = 0;
            _paletteTable = new byte[32];
            Screen = new Color[256 * 240];
            _currentRenderState = RenderState.PreRender;
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
        

        public override void Clock()
        {
            switch (_currentRenderState)
            {
                case (RenderState.PreRender):
                {
                    if (_currentCycle == 1)
                    {
                        //reset VBlank and 0Hit
                        _statusRegister.SetFlag(PpuStatusRegisterFlags.V, false);
                        _statusRegister.SetFlag(PpuStatusRegisterFlags.S, false)
                    }
                    else if (_currentCycle == ScanLineVisibleDots + 2 &&
                             _showBackground &&
                             _showSprites)
                    {
                        AddressBusAddress &= unchecked((ushort)~0x41F);
                        AddressBusAddress |= _temporaryAddressStore;
                    }
                    else if (_currentCycle > 280 && _currentCycle <= 304 && _showBackground && _showSprites)
                    {
                        AddressBusAddress &= unchecked((ushort)~0x7BE0);
                        AddressBusAddress |= (ushort)(_temporaryAddressStore & 0x7BE0);
                    }

                    
                    if (_currentCycle >= MaxPPUCycles - (_frame % 2 == 1 && _showBackground && _showSprites).ToInt())
                    {
                        _currentRenderState = RenderState.Render;
                        _currentCycle = 0;
                        _currentScanline = 0;
                    }
                    break;
                }
                case (RenderState.Render):
                {
                    if (_currentCycle is > 0 and <= ScanLineVisibleDots)
                    {
                        byte backgroundColour = 0;
                        byte spriteColour = 0;
                        bool backgroundOpaque = false;
                        bool spriteOpaque = false;
                        bool spriteForeground = false;
                        
                        int x = _currentCycle - 1;
                        int y = _currentScanline;

                        if (_showBackground)
                        {
                            var fineX = (_fineX + x) % 8;
                            
                            
                            
                            Screen[x + y * 256] = NesColor.Palette
                        }
                    }
                    break;
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

        private Color GetPaletteColour(byte palette, byte pixel)
        {
            return NesColor.Palette[Read((ushort)(0x3F00 + (palette << 2) + pixel & 0x3F))];
        }

        private void CartridgeLoaded()
        {
            if (!_cartridge.Header.NesFlags6.Mirroring)
            {
                _nametable0 = _nametable1 = 0;
                _nametable2 = _nametable3 = 0x400;
            }
            else
            {
                _nametable0 = _nametable1 = 0;
                _nametable1 = _nametable3 = 0x400;
            }
        }
    }
}