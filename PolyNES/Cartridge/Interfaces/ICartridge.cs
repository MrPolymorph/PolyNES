using System;
using Poly6502.Microprocessor.Interfaces;

namespace PolyNES.Cartridge.Interfaces
{
    public interface ICartridge : IAddressBusCompatible, IDataBusCompatible
    {
        bool CartridgeLoaded { get; }
        Header Header { get; }
        byte[] TrainerData { get; }
        byte[] ProgramRomData { get; }
        byte[] CharacterRomData { get; }

        void LoadCartridge(string file);

        void RegisterCartridgeLoadedCallback(Action callback);
        byte Peek(ushort addres);
    }
}