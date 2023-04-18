namespace PolyNES.Cartridge.Flags.INES
{
    public class INesFlags7
    {
        /// <summary>
        /// VS Unisystem
        /// </summary>
        public bool VsUnisystem { get; }
        
        /// <summary>
        /// PlayChoice-10 (8KB of Hint Screen data stored after CHR data) 
        /// </summary>
        public bool PlayChoise10 { get; }
        
        /// <summary>
        /// if equal to 2, flags 8-15 are in NES 2.0 format.
        /// </summary>
        public byte INesFormat { get; }
        
        /// <summary>
        /// Upper nibble of mapper number.
        /// </summary>
        public byte MapperHi { get; }

        public INesFlags7()
        {
            
        }

        public INesFlags7(byte data)
        {
            VsUnisystem = (data & (1 << 0)) != 0;
            PlayChoise10 = (data & (1 << 1)) != 0;
            INesFormat = (byte) ((data & (1 << 2)) | (data & (1 << 3)));
            MapperHi = (byte) (data & 0xF0);
        }
        
        public byte ToByte()
        {
            byte data = 0;
            data = (byte) ((VsUnisystem ? 1 : 0) & (1 << 0));
            data = (byte) ((PlayChoise10 ? 1 : 0) & (1 << 1));
            data = (byte) ((INesFormat) & (1 << 2) & (1 << 2));
            data = (byte) ((INesFormat) & (1 << 3)& (1 << 3));
            data = (byte) (MapperHi << 4 | data);
            
            return data;
        }
    }
}