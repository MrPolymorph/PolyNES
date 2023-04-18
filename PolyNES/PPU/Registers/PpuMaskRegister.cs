using System;

namespace PolyNES.PPU.Registers
{
    public class PpuMaskRegister
    { 
        /// <summary>
        /// Disables composite color burst (when true)
        /// effectively causes gfx to go black & white; 
        /// </summary>
        private bool _greyscale;
        
        /// <summary>
        /// left side screen column (8 pixels wide) background clipping (when false). 
        /// </summary>
        private bool _leftBackground;
        
        /// <summary>
        /// left side screen column (8 pixels whide) sprite clipping (when false) 
        /// </summary>
        private bool _leftSprites;
        
        /// <summary>
        /// Enable background display (when true)
        /// </summary>
        private bool _backgrounds;
        /// <summary>
        /// Enable sprite display (when true)
        /// </summary>
        private bool _sprites;
        private bool _red;
        private bool _green;
        private bool _blue;
        
        public byte Register
        {
            get
            {
                byte register = 0;

                register |= (byte) (_greyscale ? (1 << 7) : (0 << 7));
                register |= (byte) (_leftBackground ? (1 << 6) : (0 << 6));
                register |= (byte) (_leftSprites ? (1 << 5) : (0 << 5));
                register |= (byte) (_backgrounds ? (1 << 4) : (0 << 4));
                register |= (byte) (_sprites ? (1 << 3) : (0 << 3));
                register |= (byte) (_red ? (1 << 2) : (0 << 2));
                register |= (byte) (_green ? (1 << 1) : (0 << 1));
                register |= (byte) (_blue ? 1 : 0);
                return register;
            }
        }
        
        public bool HasFlag(PpuMaskRegisterFlags flag)
        {
            switch (flag)
            {
                case PpuMaskRegisterFlags.Greyscale:
                    return _greyscale;
                case PpuMaskRegisterFlags.LeftBackground:
                    return _leftBackground;
                case PpuMaskRegisterFlags.LeftSprites:
                    return _leftSprites;
                case PpuMaskRegisterFlags.Backgrounds:
                    return _backgrounds;
                case PpuMaskRegisterFlags.Sprites:
                    return _sprites;
                case PpuMaskRegisterFlags.Red:
                    return _red;
                case PpuMaskRegisterFlags.Green:
                    return _green;
                case PpuMaskRegisterFlags.Blue:
                    return _blue;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }
        }
        
        public void SetFlag(byte b)
        {
            _greyscale = (b & (1 << 0)) != 0;
            _leftBackground = (b & (1 << 1)) != 0;
            _leftSprites = (b & (1 << 2)) != 0;
            _backgrounds = (b & (1 << 3)) != 0;
            _sprites = (b & (1 << 4)) != 0;
            _red = (b & (1 << 5)) != 0;
            _green = (b & (1 << 6)) != 0;
            _blue = (b & (1 << 7)) != 0;
        }
        
        public void SetFlag(PpuMaskRegisterFlags flag, bool set = true)
        {
            switch (flag)
            {
                case PpuMaskRegisterFlags.Greyscale:
                    _greyscale = set;
                    break;
                case PpuMaskRegisterFlags.LeftBackground:
                    _leftBackground = set;
                    break;
                case PpuMaskRegisterFlags.LeftSprites:
                    _leftSprites = set;
                    break;
                case PpuMaskRegisterFlags.Backgrounds:
                    _backgrounds = set;
                    break;
                case PpuMaskRegisterFlags.Sprites:
                    _sprites = set;
                    break;
                case PpuMaskRegisterFlags.Red:
                    _red = set;
                    break;
                case PpuMaskRegisterFlags.Green:
                    _green = set;
                    break;
                case PpuMaskRegisterFlags.Blue:
                    _blue = set;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }
        }

        public override string ToString()
        {
            string output =  $"{(_greyscale ? "XScroll | " : string.Empty)} {(_leftBackground ? "YScroll | " : string.Empty)} {(_leftSprites ? "PpuIncrement | " : string.Empty)}"; 
                  output +=  $"{(_backgrounds ? "SpritePatternTable | " : string.Empty)} {(_sprites ? "BackgroundPatternTable | " : string.Empty)} {(_red ? "SpriteSize | " : string.Empty)}";
                  output +=  $"{(_green ? "MasterSlave | " : string.Empty)} {(_blue ? "NonMaskableInterrupt | " : string.Empty)}  0x{Register}";

            return output;
        }
    }
    
    public enum PpuMaskRegisterFlags
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
        Greyscale,

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
        LeftBackground,

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
        LeftSprites,

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
        ///         <description>$1000, ignored in 8x16 mode</description>
        ///     </item>
        /// </list>
        /// </summary>
        Backgrounds,

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
        ///         <description>$1000</description>
        ///     </item>
        /// </list>
        /// </summary>
        Sprites,

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
        Red,

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
        Green,

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
        Blue
    }
}