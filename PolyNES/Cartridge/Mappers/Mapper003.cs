using PolyNES.Cartridge.Interfaces;

namespace PolyNES.Cartridge.Mappers;

public class Mapper003 : IMapper
{
    private readonly int _numProgramBanks;
    private readonly int _numCharacterBanks;
        
    public bool Propagate { get; private set; }

    public Mapper003(int numProgramBanks, int numCharacterBanks)
    {
        _numProgramBanks = numProgramBanks;
        _numCharacterBanks = numCharacterBanks;
    }
        
    public ushort Read(ushort address)
    {
        Propagate = false;
        
        if (address is >= 0x8000 and <= 0xFFFF)
        {
            switch (_numProgramBanks)
            {
                case 1:
                    Propagate = true;
                    return (ushort) (address & 0x3FFF);
                case 2:
                    Propagate = true;
                    return (ushort) (address & 0x7FFF);
                default:
                    return 0;
            }
        }

        return 0;
    }
}