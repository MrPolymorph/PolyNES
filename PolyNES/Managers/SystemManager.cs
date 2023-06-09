using System.Collections.Generic;
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
        private readonly M6502 _m6502;
        private readonly Poly2C02 _ppu;
        private readonly ICartridge _cartridge;
        private readonly IEnumerable<AbstractAddressDataBus> _databusDevices;
        private Duration _cpuCycleDuration;
        private IClock _clock;
        private Duration _elapsed;
        private Instant _cycleTime;


        public SystemManager(M6502 microprocessor, ICartridge cartridge, Poly2C02 ppu, 
            IDataBusCompatible ram)
        {
            _m6502 = microprocessor;
            _cartridge = cartridge;
            _ppu = ppu;
            _clock = SystemClock.Instance;
            _cpuCycleDuration = Duration.FromNanoseconds(559);
            OkToDraw = false;

            //register everything with the cpu
            _m6502.RegisterDevice(cartridge);
            _m6502.RegisterDevice(ppu);
            _m6502.RegisterDevice(ram as IAddressBusCompatible);

            //register ram and cartridge together
            cartridge.RegisterDevice(ppu);
            cartridge.RegisterDevice(ram as IAddressBusCompatible);
            cartridge.RegisterCartridgeLoadedCallback(CartridgeLoaded);
            
            //register the cartridge with the ppu
            _ppu.RegisterDevice(cartridge);
            _ppu.RegisterDevice(ram as IAddressBusCompatible);
        }

        public void PowerOn()
        {
            //Powering on without a cartridge loaded should do nothing.
            if (_cartridge.CartridgeLoaded)
            {
                _m6502.RES();
                _ppu.RES();
            }
        }

        public void LoadRom(string romLocation)
        {
            _cartridge.LoadCartridge(romLocation);
            PowerOn();
            
            _cycleTime = _clock.GetCurrentInstant();
            _elapsed = _cycleTime - _cycleTime;
        }
        
        public bool ClockSystem()
        {
            // _elapsed += _clock.GetCurrentInstant() - _cycleTime;
            // _cycleTime = _clock.GetCurrentInstant();
            //
            // while (_elapsed > _cpuCycleDuration)
            // {
                _ppu.Clock();
                _ppu.Clock();
                _ppu.Clock();
                _m6502.Clock();

            //     OkToDraw = _ppu.FrameComplete;
            //     _elapsed -= _cpuCycleDuration;
            // }
            //
            // OkToDraw = false;
            return true;
        }

        private void CartridgeLoaded()
        {
            _m6502.RES();
            _ppu.RES();
        }
        
    }
}