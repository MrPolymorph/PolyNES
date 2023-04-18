using System;

namespace PolyNES.PPU.Registers
{
    public class PpuStatusRegister
    {
        /// <summary>
        /// Verticle blank has started (0: not in vblank; 1: in vblank).
        /// Set at dot 1 of line 241 (the line *after* the post render line);
        /// cleared after reading $2002 and at dot 1 of the pre-render line.
        /// </summary>
        private bool VerticalBlank;
        /// <summary>
        /// Sprite 0 Hit.  Set when a nonzero pixel of sprite 0 overlaps
        /// a nonzero background pixel; cleared at dot 1 of the pre-render
        /// line.  Used for raster timing.
        /// </summary>
        private bool SpriteZeroHit;
        /// <summary>
        /// Sprite overflow. The intent was for this flag to be set
        /// whenever more than eight sprites appear on a scanline, but a
        /// hardware bug causes the actual behavior to be more complicated
        /// and generate false positives as well as false negatives; see
        /// PPU sprite evaluation. This flag is set during sprite
        /// evaluation and cleared at dot 1 (the second dot) of the
        /// pre-render line.
        /// </summary>
        private bool SpriteOverflow;

        /// <summary>
        /// Least significant bits previously written into a PPU register
        /// (due to register not being updated for this address)
        /// </summary>
        private bool Byte4;

        private bool Byte3;
        private bool Byte2;
        private bool Byte1;
        private bool Byte0;

        public byte Register
        {
            get
            {
                byte register = 0;
                register |= (byte) (VerticalBlank ? (1 << 7) : (0 << 7));
                register |= (byte) (SpriteZeroHit ? (1 << 6) : (0 << 6));
                register |= (byte) (SpriteOverflow ? (1 << 5) : (0 << 5));
                register |= (byte) (Byte4 ? (1 << 4) : (0 << 4));
                register |= (byte) (Byte3 ? (1 << 3) : (0 << 3));
                register |= (byte) (Byte2 ? (1 << 2) : (0 << 2));
                register |= (byte) (Byte1 ? (1 << 1) : (0 << 1));
                register |= (byte)(Byte0 ? 1 : 0);
                return register;
            }
        }
        
        public bool HasFlag(PpuStatusRegisterFlags flag)
        {
            switch (flag)
            {
                case PpuStatusRegisterFlags.V:
                    return VerticalBlank;
                case PpuStatusRegisterFlags.S:
                    return SpriteZeroHit;
                case PpuStatusRegisterFlags.O:
                    return SpriteOverflow;
                case PpuStatusRegisterFlags.Byte4:
                    return Byte4;
                case PpuStatusRegisterFlags.Byte3:
                    return Byte3;
                case PpuStatusRegisterFlags.Byte2:
                    return Byte2;
                case PpuStatusRegisterFlags.Byte1:
                    return Byte1;
                case PpuStatusRegisterFlags.Byte0:
                    return Byte0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }
        }
        
        public void SetFlag(byte b)
        {
            Byte0 = (b & (1 << 0)) != 0;
            Byte1 = (b & (1 << 1)) != 0;
            Byte2 = (b & (1 << 2)) != 0;
            Byte3 = (b & (1 << 3)) != 0;
            Byte4 = (b & (1 << 4)) != 0;
            SpriteOverflow = (b & (1 << 5)) != 0;
            SpriteZeroHit = (b & (1 << 6)) != 0;
            VerticalBlank = (b & (1 << 7)) != 0;
        }
        
        public bool SetFlag(PpuStatusRegisterFlags flag, bool set = true)
        {
            switch (flag)
            {
                case PpuStatusRegisterFlags.V:
                    VerticalBlank = set;
                    break;
                case PpuStatusRegisterFlags.S:
                    SpriteZeroHit = set;
                    break;
                case PpuStatusRegisterFlags.O:
                    SpriteOverflow = set;
                    break;
                case PpuStatusRegisterFlags.Byte4:
                    Byte4 = set;
                    break;
                case PpuStatusRegisterFlags.Byte3:
                    Byte3 = set;
                    break;
                case PpuStatusRegisterFlags.Byte2:
                    Byte2 = set;
                    break;
                case PpuStatusRegisterFlags.Byte1:
                    Byte1 = set;
                    break;
                case PpuStatusRegisterFlags.Byte0:
                    Byte0 = set;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }

            return set;
        }
        
        public override string ToString()
        {
            string output =  $"{(VerticalBlank ? "V | " : string.Empty)} {(SpriteZeroHit ? "S | " : string.Empty)} {(SpriteOverflow ? "O | " : string.Empty)}"; 
            output +=  $"{(Byte4 ? "Byte4 | " : string.Empty)} {(Byte3 ? "Byte3 | " : string.Empty)} {(Byte2 ? "Byte2 | " : string.Empty)}";
            output +=  $"{(Byte1 ? "byte1 | " : string.Empty)} {(Byte0 ? "N | " : string.Empty)}  0x{Register}";

            return output;
        }

        public void Reset()
        {
            Byte0 = false;
            Byte1 = false;
            Byte2 = false;
            Byte3 = false;
            Byte4 = false;
            SpriteOverflow = false;
            SpriteZeroHit = false;
            VerticalBlank = false;
        }
    }

    public enum PpuStatusRegisterFlags
    {
        /// <summary>
        /// Verticle blank has started (0: not in vblank; 1: in vblank).
        /// Set at dot 1 of line 241 (the line *after* the post render line);
        /// cleared after reading $2002 and at dot 1 of the pre-render line.
        /// </summary>
        V,
        /// <summary>
        /// Sprite 0 Hit.  Set when a nonzero pixel of sprite 0 overlaps
        /// a nonzero background pixel; cleared at dot 1 of the pre-render
        /// line.  Used for raster timing.
        /// </summary>
        S,
        /// <summary>
        /// Sprite overflow. The intent was for this flag to be set
        /// whenever more than eight sprites appear on a scanline, but a
        /// hardware bug causes the actual behavior to be more complicated
        /// and generate false positives as well as false negatives; see
        /// PPU sprite evaluation. This flag is set during sprite
        /// evaluation and cleared at dot 1 (the second dot) of the
        /// pre-render line.
        /// </summary>
        O,
        /// <summary>
        /// Least significant bits previously written into a PPU register
        /// (due to register not being updated for this address)
        /// </summary>
        Byte4,
        Byte3,
        Byte2,
        Byte1,
        Byte0
    }
}

