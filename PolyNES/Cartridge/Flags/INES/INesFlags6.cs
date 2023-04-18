namespace PolyNES.Cartridge.Flags.INES
{
    public class INesFlags6
    {
        /// <summary>
        /// Mirroring:
        ///
        /// if 0 then horizontal (vertical arrangement) (CIRAM A10 = PPU A11)
        /// if 1 then vertical (horizontal arrangement) (CIRAM A10 = PPU A10)
        /// </summary>
        public bool Mirroring { get; }
        
        /// <summary>
        /// Cartridge contains battery-backed PRG RAM ($6000-7FFF) or other persistent memory
        /// </summary>
        public bool ExtendedRam { get; }
        
        /// <summary>
        /// 512-byte trainer at $7000-$71FF (stored before PRG data)
        /// </summary>
        public bool HasTrainerData { get; }
        
        /// <summary>
        /// Ignore mirroring control or above mirroring bit; instead provide four-screen VRAM
        /// </summary>
        public bool IgnoreMirroring { get; }
        
        /// <summary>
        /// Lower Nibble of mapper number
        /// </summary>
        public byte MapperLo { get; }

        public INesFlags6()
        {
            
        }

        public INesFlags6(byte data)
        {
            Mirroring = (data & (1 << 0)) != 0;
            ExtendedRam = (data & (1 << 1)) != 0;
            HasTrainerData = (data & (1 << 2)) != 0;
            IgnoreMirroring = (data & (1 << 3)) != 0;
            MapperLo = (byte) (data & 0xF0);
        }
        
        public byte ToByte()
        {
            byte data = 0;
            data = (byte) ((Mirroring ? 1 : 0) & (1 << 0));
            data = (byte) ((ExtendedRam ? 1 : 0) & (1 << 1));
            data = (byte) ((HasTrainerData ? 1 : 0) & (1 << 2));
            data = (byte) ((IgnoreMirroring ? 1 : 0) & (1 << 3));
            data = (byte) (MapperLo << 4 | data);
            
            return data;
        }
    }
}