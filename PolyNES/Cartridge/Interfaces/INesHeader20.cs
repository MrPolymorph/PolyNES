namespace PolyNES.Cartridge.Interfaces
{
    public interface INesHeader20
    {
        byte[] Identification { get; }
        int ProgramRomSize { get; }
        int CharacterRomSize { get; }
    }
}