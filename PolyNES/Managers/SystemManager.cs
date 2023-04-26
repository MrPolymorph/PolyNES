using System.Collections.Generic;
using System.Linq;
using NodaTime;
using Poly6502.Microprocessor;
using Poly6502.Microprocessor.Interfaces;
using Poly6502.Microprocessor.Utilities;
using PolyNES.Cartridge.Interfaces;
using PolyNES.Interfaces;
using PolyNES.PPU;

namespace PolyNES.Managers
{
    public class SystemManager : ISystemManager
    {
        public bool OkToDraw;
        public bool Run;
        public readonly M6502 M6502;
        public List<string> Disassembly;
        public readonly Poly2C02 PPU;
        private readonly ICartridge _cartridge;
        private readonly IEnumerable<AbstractAddressDataBus> _databusDevices;
        private Duration _cpuCycleDuration;
        private IClock _clock;
        private Duration _elapsed;
        private Instant _cycleTime;


        public SystemManager(M6502 microprocessor, ICartridge cartridge, Poly2C02 ppu, 
            IDataBusCompatible ram)
        {
            M6502 = microprocessor;
            _cartridge = cartridge;
            PPU = ppu;
            _clock = SystemClock.Instance;
            _cpuCycleDuration = Duration.FromNanoseconds(559);
            OkToDraw = false;

            //register everything with the cpu
            M6502.RegisterDevice(cartridge);
            M6502.RegisterDevice(ppu);
            M6502.RegisterDevice(ram as IAddressBusCompatible);

            //register ram and cartridge together
            cartridge.RegisterDevice(ppu);
            cartridge.RegisterDevice(ram as IAddressBusCompatible);
            cartridge.RegisterCartridgeLoadedCallback(CartridgeLoaded);
            
            //register the cartridge with the ppu
            PPU.RegisterDevice(cartridge);
            PPU.RegisterDevice(ram as IAddressBusCompatible);
        }

        public void PowerOn()
        {
            //Powering on without a cartridge loaded should do nothing.
            if (_cartridge.CartridgeLoaded)
            {
                M6502.RES();
                PPU.RES();
            }
        }

        public void LoadRom(string romLocation)
        {
            _cartridge.LoadCartridge(romLocation);
            Disassembly = M6502.Disassemble(0xC000, 0xFB77).ToList();
            PowerOn();
            
            _cycleTime = _clock.GetCurrentInstant();
            _elapsed = _cycleTime - _cycleTime;
        }

        public void Reset()
        {
            M6502.RES(0xC000);
        }

        public void StepSystem()
        {
            PPU.Clock();
            PPU.Clock();
            PPU.Clock();
            M6502.Clock();
            PPU.DrawPatternTable(0, 0);
            PPU.DrawPatternTable(1, 0);
        }
        
        public bool ClockSystem()
        {
            // _elapsed += _clock.GetCurrentInstant() - _cycleTime;
            // _cycleTime = _clock.GetCurrentInstant();
            //
            // while (_elapsed > _cpuCycleDuration)
            // {
            if (Run)
            {
                PPU.Clock();
                PPU.Clock();
                PPU.Clock();
                M6502.Clock();
                PPU.DrawPatternTable(0, 0);
                PPU.DrawPatternTable(1, 0);
            }

            //     OkToDraw = _ppu.FrameComplete;
            //     _elapsed -= _cpuCycleDuration;
            // }
            //
            // OkToDraw = false;
            return true;
        }

        private void CartridgeLoaded()
        {
            M6502.RES();
            PPU.RES();
        }
        
    }
}