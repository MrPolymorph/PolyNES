using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Poly6502.Microprocessor.Utilities;
using PolyNES.Cartridge.Flags.INES;
using PolyNES.Cartridge.Interfaces;
using PolyNES.Cartridge.Mappers;
using PolyNES.PPU.Data;

namespace PolyNES.Cartridge
{
    /// <summary>
    /// 
    /// </summary>
    public class Cartridge : AbstractAddressDataBus, ICartridge
    {
        private const int LeftPatternTableStartAddress = 0x000;
        private const int LeftPatternTableEndAddress = 0x0FFF;

        private const int RightPatternTableStartAddress = 0x1000;
        private const int RightPatternTableEndAddress = 0x1FFF;
        
        private readonly ICartridge _cartridge;
        private byte[] _cartridgeData;
        private bool _characterRamInUse;

        private IList<Action> _loadedObservers;
        public IMapper Mapper { get; private set; }

        public Header Header { get; }
        public byte[] TrainerData { get; private set; }
        public byte[] ProgramRomData { get; private set; }
        /// <summary>
        /// This includes the pattern tables for the sprites and background tiles,
        /// as well as any other graphics data that the game uses
        /// </summary>
        public byte[] CharacterRomData { get; private set; }
        public byte[] CharacterRamData { get; private set; }
        public bool CartridgeLoaded { get; private set; }

        public byte[] LeftPatternTable;
        public byte[] RightPatternTable;

        public Color[] LeftPatternTableBuffer;
        public Color[] RightPatternTableBuffer;

        public Color[] CombinedPatternTable;
        
        public Cartridge()
        {
            MinAddressableRange = 0x4020;
            MaxAddressableRange = 0xFFFF;
            LeftPatternTable = new byte[LeftPatternTableEndAddress];
            RightPatternTable = new byte[LeftPatternTableEndAddress];
            CombinedPatternTable = new Color[0xFFFFF];
            Header = new Header();

            _loadedObservers = new List<Action>();
            _characterRamInUse = false;
        }

        public void LoadCartridge(string file)
        {
            try
            {
                if (!string.IsNullOrEmpty(file) && !string.IsNullOrWhiteSpace(file))
                {
                    _cartridgeData = File.ReadAllBytes(file);

                    bool iNesFormat = _cartridgeData[0] == 'N' && _cartridgeData[1] == 'E' &&
                                      _cartridgeData[2] == 'S' &&
                                      _cartridgeData[3] == 0x1A;
                    bool nes20Format = iNesFormat && (_cartridgeData[7] & 0x0C) == 0x08;

                    if (nes20Format)
                        ParseINes20Format();
                    else
                        ParseINesFormat(_cartridgeData);

                    CartridgeLoaded = true;
                }
            }
            catch (Exception aex)
            {
                throw;
            }
        }

        public void RegisterCartridgeLoadedCallback(Action callback)
        {
            if (!_loadedObservers.Contains(callback))
            {
                _loadedObservers.Add(callback);
            }
        }

        private void ParseINesFormat(byte[] cartData)
        {
            //Header 16 Bytes
            Header.Identifier = new[]{(char)cartData[0], (char)cartData[1], (char)cartData[2], (char)cartData[3] };
            Header.NumberOfProgramBanks = cartData[4];
            Header.NumberOfCharacterBanks = cartData[5];
            Header.NesFlags6 = new INesFlags6(cartData[6]);
            Header.NesFlags7 = new INesFlags7(cartData[7]);
            Header.ProgramRamSize = cartData[8];
            Header.PAL = cartData[9] != 0;
            Header.Flags10 = new INesFlags10(cartData[10]);


            if (Header.NesFlags6.HasTrainerData)
            {
                TrainerData = cartData[11..512];
            }

            //Start of array range is inclusive, but end is exclusive
            var romDataEnd = 16 + (Header.NumberOfProgramBanks * 0x4000); 
            
            ProgramRomData = cartData[16..romDataEnd];

            var charRomSize = 0;

            if (Header.NumberOfCharacterBanks != 0)
            {
                charRomSize = romDataEnd + Header.NumberOfCharacterBanks * 0x2000;
                CharacterRomData = cartData[romDataEnd..charRomSize];
                _characterRamInUse = false;
            }
            else
            {
                _characterRamInUse = true;
                CharacterRamData = new byte[0x2000];
            }


            byte[] titleBytes = cartData[(cartData.Length - 5)..(cartData.Length)];
            var title = System.Text.Encoding.UTF8.GetString(titleBytes);

            int mapperId = cartData[6] >> 4 | cartData[7] & 0xF0;

            switch (mapperId)
            {
                case (0):
                    Mapper = new Mapper000(Header.NumberOfProgramBanks, Header.NumberOfCharacterBanks);
                    break;
                case (3):
                    Mapper = new Mapper003(Header.NumberOfProgramBanks, Header.NumberOfCharacterBanks);
                    break;
                default:
                    Mapper = new Mapper000(Header.NumberOfProgramBanks, Header.NumberOfCharacterBanks);
                    break;
            }

            //Fill the pattern tables
            LeftPatternTable = CharacterRomData[LeftPatternTableStartAddress..LeftPatternTableEndAddress];
            RightPatternTable = CharacterRomData[RightPatternTableStartAddress..RightPatternTableEndAddress];
            
            for (int r = 0; r < 256; r++) {
                for (int col = 0; col < 128; col++) {
                    int adr = (r / 8 * 0x100) + (r % 8) + (col / 8) * 0x10;
                    int pixel = ((CharacterRomData[adr] >> (7-(col % 8))) & 1) + ((CharacterRomData[adr + 8] >> (7-(col % 8))) & 1) * 2;

                    var index1 = (r * 128 * 3) + (col * 3);
                    var index2 = (r * 128 * 3) + (col * 3) + 1;
                    var index3 = (r * 128 * 3) + (col * 3) + 2;

                    var something = 1;
                    
                    if (index1 >= CombinedPatternTable.Length ||
                        index2 >= CombinedPatternTable.Length ||
                        index3 >= CombinedPatternTable.Length)
                    {
                        something = 1;
                    }

                    something = 2 + something;
                    
                    CombinedPatternTable[index1]     = NesColor.Palette[pixel];
                    CombinedPatternTable[index2] = NesColor.Palette[pixel];
                    CombinedPatternTable[index3] = NesColor.Palette[pixel];
                }
            }
            
            CartridgeLoaded = true;

            foreach (var observer in _loadedObservers)
            {
                observer();
            }
        }

        private void ParseINes20Format()
        {
        }

        public override byte Read(ushort address, bool ronly = false)
        {
            if (!CartridgeLoaded)
                return 0;
            
            SetPropagation(false);
            
            if (address >= MinAddressableRange && address <= MaxAddressableRange)
            {
                var mapped = Mapper.Read(address);

                if (Mapper.Propagate)
                {
                    SetPropagation(true);
                    return address <= 0x1FFF ? CharacterRomData[mapped] : ProgramRomData[mapped];
                }
            }

            return 0;
        }

        public override void Write(ushort address, byte data)
        {
            if (!CartridgeLoaded)
                return;

            if (_characterRamInUse)
                CharacterRamData[address] = data;
            else{}
                //log
        }

        public override void Clock()
        {
        }

        public override void SetRW(bool rw)
        {
        }

        public byte Peek(ushort address)
        {
            if (address >= MinAddressableRange && address <= MaxAddressableRange)
            {
                var mapped = Mapper.Read(address);

                if (Mapper.Propagate)
                {
                    return address <= 0x1FFF ? CharacterRomData[mapped] : ProgramRomData[mapped];
                }
            }

            return 0;
        }
    }
}