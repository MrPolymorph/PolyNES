using PolyNES.Cartridge.Flags.INES;
using PolyNES.Cartridge.Interfaces;

namespace PolyNES.Cartridge
{
    public class Header : INesHeader
    {
        public char[] Identifier { get; set; }
        public byte NumberOfProgramBanks { get; set; }
        public byte NumberOfCharacterBanks { get; set; }
        public INesFlags6 NesFlags6 { get; set; }
        public INesFlags7 NesFlags7 { get; set; }
        public byte ProgramRamSize { get; set; }
        public bool PAL { get; set; }
        public INesFlags10 Flags10 { get; set; }

        public Header()
        {
            Identifier = new char[4];
        }
        
    }
}