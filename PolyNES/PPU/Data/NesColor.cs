using Microsoft.Xna.Framework;

namespace PolyNES.PPU.Data
{
    /// <summary>
    /// Representation of a NES pallete created with Bisqwit;s palette generator.
    /// </summary>
    public static class NesColor
    {
        public static Color[] Palette;
        static NesColor()
        {
            Palette = new[] {
                /* 00 */ new Color(101,101,101),
                /* 01 */ new Color(0,45,105),
                /* 02 */ new Color(19,31,127),
                /* 03 */ new Color(0, 60,19,124),
                /* 04 */ new Color(96,11,98),
                /* 05 */ new Color(113,15,7),
                /* 06 */ new Color(113,15,7),
                /* 07 */ new Color(90,26,0),
                /* 08 */ new Color(52,40,0),
                /* 09 */ new Color(11,52,0),
                /* 0A */ new Color(0,60,0),
                /* 0B */ new Color(0,61,16),
                /* 0C */ new Color(0,56,64),
                /* 0D */ new Color(0,0,0),
                /* 0E */ new Color(0,0,0),
                /* 0F */ new Color(0,0,0),
            
                /* 10 */ new Color(174,174,174),
                /* 11 */ new Color(15,99,179),
                /* 12 */ new Color(64,81,208),
                /* 13 */ new Color(120,65,204),
                /* 14 */ new Color(167,65,169),
                /* 15 */ new Color(192,52,112),
                /* 16 */ new Color(189,60,48),
                /* 17 */ new Color(159,74,0),
                /* 18 */ new Color(109,92,0),
                /* 19 */ new Color(54,109,0),
                /* 1A */ new Color(7,119,4),
                /* 1B */ new Color(0,121,61),
                /* 1C */ new Color(0,114,125),
                /* 1D */ new Color(0,0,0),
                /* 1E */ new Color(0,0,0),
                /* 1F */ new Color(0,0,0),
            
                /* 20 */ new Color(254,254,255),
                /* 21 */ new Color(93,179,255),
                /* 22 */ new Color(143,161,255),
                /* 23 */ new Color(200,144,255),
                /* 24 */ new Color(247,133,250),
                /* 25 */ new Color(255,131,192),
                /* 26 */ new Color(255,139,127),
                /* 27 */ new Color(239,154,73),
                /* 28 */ new Color(189,172,44),
                /* 29 */ new Color(133,188,47),
                /* 2A */ new Color(85,199,83),
                /* 2B */ new Color(60,201,140),
                /* 2C */ new Color(62,194,205),
                /* 2D */ new Color(78,78,78),
                /* 2E */ new Color(0,0,0),
                /* 2F */ new Color(0,0,0),
            
                /* 30 */ new Color(254,254,255),
                /* 31 */ new Color(188,223,255),
                /* 32 */ new Color(209,216,255),
                /* 33 */ new Color(232,209,255),
                /* 34 */ new Color(251,205,253),
                /* 35 */ new Color(255,204,229),
                /* 36 */ new Color(255,207,202),
                /* 37 */ new Color(248,213,180),
                /* 38 */ new Color(228,220,168),
                /* 39 */ new Color(204,227,169),
                /* 3A */ new Color(185,232,184),
                /* 3B */ new Color(174,232,208),
                /* 3C */ new Color(175,229,234),
                /* 3D */ new Color(182,182,182),
                /* 3E */ new Color(0,0,0),
                /* 3F */ new Color(0,0,0),
            };
        }

        public static Color ColorFromPalette(int x, int y)
        {
            return Palette[16 * x + y];
        }
    }
}