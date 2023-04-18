using PolyNES.Cartridge.Interfaces;

namespace PolyNES.Cartridge.Mappers
{
    public class Mapper000 : IMapper
    {
        private readonly int _numProgramBanks;
        private readonly int _numCharacterBanks;
        
        public bool Propagate { get; private set; }

        public Mapper000(int numProgramBanks, int numCharacterBanks)
        {
            _numProgramBanks = numProgramBanks;
            _numCharacterBanks = numCharacterBanks;
        }
        
        public ushort Read(ushort address)
        {
            Propagate = false;
            
            switch (address)
            {
                case >= 0x0000 and <= 0x1FFF: //From the PPU
                    Propagate = true;
                    return address;
                case >= 0x8000 and <= 0xFFFF: //From the CPU
                    Propagate = true;
                    if (_numProgramBanks > 1)
                    {
                        return 0;
                    }
                    else
                    {
                        return (ushort) ((address - 0x8000) & 0x3FFF);
                    }
                    break;
                default:
                    return 0;
            }
        }
    }
}