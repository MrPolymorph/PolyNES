namespace PolyNES.Cartridge.Flags.INES_2._0
{
    public class INes20Flags6
    {
        /// <summary>
        /// Hard-Wired nametable mirroring type
        ///
        /// if false then Horizontal or mapper-controlled
        /// if true then Vertical.
        /// </summary>
        public bool NameTableMirroring { get; }
        
        /// <summary>
        /// Battery and other non volatile memory
        ///
        /// if false then no present
        /// if ture then present
        /// </summary>
        public bool BatteryBackedMemory { get; }
        
        /// <summary>
        /// 512-byte Trainer data
        /// 
        /// if false then no present
        /// if true then present
        /// </summary>
        public bool TrainerDataPresent { get; }
        
        /// <summary>
        /// if 0 then no
        /// if true then yes.
        /// </summary>
        public bool FourScreenMode { get; }
        
        public byte MapperNumber { get; }
    }
}