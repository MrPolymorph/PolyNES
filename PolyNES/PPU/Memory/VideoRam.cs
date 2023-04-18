using Poly6502.Microprocessor.Utilities;

namespace PolyNES.PPU.Memory;

/// <summary>
/// This 2Kb ram is used to store the Nametables
/// </summary>
public class VideoRam : AbstractAddressDataBus
{
    private const int RamSize = 0x1000;
    private byte[] _ram;
    
    public byte this[int i]
    {
        get { return _ram[i]; }
        set { _ram[i] = value; }
    }
    
    public VideoRam()
    {
        MinAddressableRange = 0x2000;
        MaxAddressableRange = 0x2FFF;
        
        _ram = new byte[RamSize];
        
        for (int i = 0; i < RamSize; i++)
        {
            _ram[i] = 0;
        }
    }
    
    public override byte Read(ushort address, bool ronly = false)
    {
        return _ram[address];
    }

    public override void Write(ushort address, byte data)
    {
        _ram[address] = data;
    }

    public override void Clock()
    {
        return;
    }

    public override void SetRW(bool rw)
    {
        return;
    }
}