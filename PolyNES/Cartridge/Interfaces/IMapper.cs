namespace PolyNES.Cartridge.Interfaces
{
    public interface IMapper
    {
        bool Propagate { get; }
        ushort Read(ushort address);
    }
}