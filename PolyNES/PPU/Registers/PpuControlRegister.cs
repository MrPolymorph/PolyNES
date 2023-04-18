using System;

namespace PolyNES.PPU.Registers
{
    public class PpuControlRegister
    {
        /// <summary>
        /// Base Name table Address
        ///
        /// X scroll name table selection.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false</term>
        ///         <description>= $2000</description>
        ///     </item>
        ///     <item>
        ///         <term>true</term>
        ///         <description>= $2400</description>
        ///     </item>
        /// </list>
        /// </summary>
        private bool _xScroll;

        /// <summary>
        /// Base Name table Address
        ///
        /// Y scroll name table selection.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false</term>
        ///         <description>= $2800</description>
        ///     </item>
        ///     <item>
        ///         <term>true</term>
        ///         <description>= $2C00</description>
        ///     </item>
        /// </list>
        /// </summary>
        private bool _yScroll;

        /// <summary>
        /// VRAM address increment per CPU read/write of PPUData
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>Add 1, going across</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>Add 32, going down</description>
        ///     </item>
        /// </list>
        /// </summary>
        private bool _ppuIncrement;

        /// <summary>
        /// Sprite pattern table address for 8x8 sprites.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>$0000</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>$1000, ignored in 8x16 mode</description>
        ///     </item>
        /// </list>
        /// </summary>
        private bool _spritePatternTableAddress;

        /// <summary>
        /// Background pattern table address.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>$0000</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>$1000</description>
        ///     </item>
        /// </list>
        /// </summary>
        private bool _backgroundPatternTableAddress;

        /// <summary>
        /// Sprite Size
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>8x8 pixels</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>8x16 pixels</description>
        ///     </item>
        /// </list>
        /// </summary>
        private bool _spriteSize;

        /// <summary>
        /// PPU master/slave select.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>read backdrop from EXT pins</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>output color on EXT pins</description>
        ///     </item>
        /// </list>
        /// </summary>
        private bool _masterSlave;

        /// <summary>
        /// Generate a NMI at the start of the
        /// vertical blanking interval
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>off</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>on</description>
        ///     </item>
        /// </list>
        /// </summary>
        private bool _nonMaskableInterrupt;
        
        public byte Register
        {
            get
            {
                byte register = 0;

                register |= (byte) (_nonMaskableInterrupt ? (1 << 7) : (0 << 7));
                register |= (byte) (_masterSlave ? (1 << 6) : (0 << 6));
                register |= (byte) (_spriteSize ? (1 << 5) : (0 << 5));
                register |= (byte) (_backgroundPatternTableAddress ? (1 << 4) : (0 << 4));
                register |= (byte) (_spritePatternTableAddress ? (1 << 3) : (0 << 3));
                register |= (byte) (_ppuIncrement ? (1 << 2) : (0 << 2));
                register |= (byte) (_yScroll ? (1 << 1) : (0 << 1));
                register |= (byte) (_xScroll ? 1 : 0);
                return register;
            }
        }

        public ushort Address()
        {
            ushort addressBase = 0x2000;
            
            switch (Register & 0x3)
            {
                case 1: addressBase += 0x400;
                    break;
                case 2: addressBase += 0x800;
                    break;
                case 3: addressBase += 0xC00;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return addressBase;
        }

        public bool HasFlag(PpuControlRegisterFlags flag)
        {
            switch (flag)
            {
                case PpuControlRegisterFlags.XScroll:
                    return _xScroll;
                case PpuControlRegisterFlags.YScroll:
                    return _yScroll;
                case PpuControlRegisterFlags.PpuIncrement:
                    return _ppuIncrement;
                case PpuControlRegisterFlags.SpritePatternTableAddress:
                    return _spritePatternTableAddress;
                case PpuControlRegisterFlags.BackgroundPatternTableAddress:
                    return _backgroundPatternTableAddress;
                case PpuControlRegisterFlags.SpriteSize:
                    return _spriteSize;
                case PpuControlRegisterFlags.MasterSlave:
                    return _masterSlave;
                case PpuControlRegisterFlags.NonMaskableInterrupt:
                    return _nonMaskableInterrupt;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }
        }

        /// <summary>
        /// Set the Register using byte data
        /// </summary>
        /// <param name="b"></param>
        public void SetFlag(byte b)
        {
            _xScroll = (b & (1 << 0)) != 0;
            _yScroll = (b & (1 << 1)) != 0;
            _ppuIncrement = (b & (1 << 2)) != 0;
            _spritePatternTableAddress = (b & (1 << 3)) != 0;
            _backgroundPatternTableAddress = (b & (1 << 4)) != 0;
            _spriteSize = (b & (1 << 5)) != 0;
            _masterSlave = (b & (1 << 6)) != 0;
            _nonMaskableInterrupt = (b & (1 << 7)) != 0;
        }
        
        public void SetFlag(PpuControlRegisterFlags flag, bool set = true)
        {
            switch (flag)
            {
                case PpuControlRegisterFlags.XScroll:
                    _xScroll = set;
                    break;
                case PpuControlRegisterFlags.YScroll:
                    _yScroll = set;
                    break;
                case PpuControlRegisterFlags.PpuIncrement:
                    _ppuIncrement = set;
                    break;
                case PpuControlRegisterFlags.SpritePatternTableAddress:
                    _spritePatternTableAddress = set;
                    break;
                case PpuControlRegisterFlags.BackgroundPatternTableAddress:
                    _backgroundPatternTableAddress = set;
                    break;
                case PpuControlRegisterFlags.SpriteSize:
                    _spriteSize = set;
                    break;
                case PpuControlRegisterFlags.MasterSlave:
                    _masterSlave = set;
                    break;
                case PpuControlRegisterFlags.NonMaskableInterrupt:
                    _nonMaskableInterrupt = set;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }
        }

        public override string ToString()
        {
            string output =  $"{(_xScroll ? "XScroll | " : string.Empty)} {(_yScroll ? "YScroll | " : string.Empty)} {(_ppuIncrement ? "PpuIncrement | " : string.Empty)}"; 
                  output +=  $"{(_spritePatternTableAddress ? "SpritePatternTable | " : string.Empty)} {(_backgroundPatternTableAddress ? "BackgroundPatternTable | " : string.Empty)} {(_spriteSize ? "SpriteSize | " : string.Empty)}";
                  output +=  $"{(_masterSlave ? "MasterSlave | " : string.Empty)} {(_nonMaskableInterrupt ? "NonMaskableInterrupt | " : string.Empty)}  0x{Register}";

            return output;
        }
    }

    public enum PpuControlRegisterFlags
    {
        /// <summary>
        /// Base Name table Address
        ///
        /// X scroll name table selection.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false</term>
        ///         <description>= $2000</description>
        ///     </item>
        ///     <item>
        ///         <term>true</term>
        ///         <description>= $2400</description>
        ///     </item>
        /// </list>
        /// </summary>
        XScroll,

        /// <summary>
        /// Base Name table Address
        ///
        /// Y scroll name table selection.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false</term>
        ///         <description>= $2800</description>
        ///     </item>
        ///     <item>
        ///         <term>true</term>
        ///         <description>= $2C00</description>
        ///     </item>
        /// </list>
        /// </summary>
        YScroll,

        /// <summary>
        /// VRAM address increment per CPU read/write of PPUData
        ///
        /// 0: add 1, going across; 1: add 32, going down
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>Add 1, going across</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>Add 32, going down</description>
        ///     </item>
        /// </list>
        /// </summary>
        PpuIncrement,

        /// <summary>
        /// Sprite pattern table address for 8x8 sprites.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>$0000</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>$1000, ignored in 8x16 mode</description>
        ///     </item>
        /// </list>
        /// </summary>
        SpritePatternTableAddress,

        /// <summary>
        /// Background pattern table address.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>$0000</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>$1000</description>
        ///     </item>
        /// </list>
        /// </summary>
        BackgroundPatternTableAddress,

        /// <summary>
        /// Sprite Size
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>8x8 pixels</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>8x16 pixels</description>
        ///     </item>
        /// </list>
        /// </summary>
        SpriteSize,

        /// <summary>
        /// PPU master/slave select.
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>read backdrop from EXT pins</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>output color on EXT pins</description>
        ///     </item>
        /// </list>
        /// </summary>
        MasterSlave,

        /// <summary>
        /// Generate a NMI at the start of the
        /// vertical blanking interval
        ///
        /// <list type="text">
        ///     <item>
        ///         <term>false :</term>
        ///         <description>off</description>
        ///     </item>
        ///     <item>
        ///         <term>true :</term>
        ///         <description>on</description>
        ///     </item>
        /// </list>
        /// </summary>
        NonMaskableInterrupt
    }
}
