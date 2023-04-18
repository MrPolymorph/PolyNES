namespace PolyNES.Cartridge.Flags.INES
{
    public class INesFlags10
    {
        
        public byte TvSystem { get; }
        public bool ProgramRamPresent { get; }
        public bool BusConflicts { get; }

        public INesFlags10()
        {
            
        }

        public INesFlags10(byte data)
        {
            TvSystem = (byte) ((data & 1 << 0) | (data & 1 << 1));
            ProgramRamPresent = (data & 1 << 4) != 0;
            BusConflicts = (data & 1 << 5) != 0;
        }
        
        public byte ToByte()
        {
            byte data = 0;
            data = (byte) ((TvSystem & (1 << 0)));
            data = (byte) ((TvSystem & (1 << 1)));
            data = (byte) ((ProgramRamPresent ? 1 : 0) & (1 << 4));
            data = (byte) (BusConflicts ? 0 : 1 & 1 << 5);

            return data;
        }
    }
    
}