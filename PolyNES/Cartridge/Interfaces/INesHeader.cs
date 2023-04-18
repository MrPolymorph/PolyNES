using PolyNES.Cartridge.Flags.INES;

namespace PolyNES.Cartridge.Interfaces
{
    public interface INesHeader
    {
        char[] Identifier { get; }
        byte NumberOfProgramBanks { get; }
        byte NumberOfCharacterBanks { get; }
        INesFlags6 NesFlags6 { get; }
        INesFlags7 NesFlags7 { get; }
        
        /// <summary>
        /// Size of Program RAm in 8KB units (Value 0 infers 8KB for compatibility).
        ///
        /// This was a later extension to the iNES format and not widely used. NES 2.0 is recommended
        /// for specifying program RAM size instead.
        /// </summary>
        byte ProgramRamSize { get; }
        
        /// <summary>
        /// TV System
        ///
        /// if false then NTSC, otherwise PAL
        /// </summary>
        bool PAL { get; }
        
    }
}