namespace PolyNES.PPU.Registers
{
    /// <summary>
    /// The high 5 bits of the register store the vertical scroll offset, and the low 3 bits store the horizontal scroll
    /// offset. By updating these values, the PPU can render different parts of the nametable on the screen.
    /// </summary>
    public class PpuScrollRegister
    {
        public byte CoarseX { get; set; }
        public byte CoarseY { get; set; }
        public byte NametableX { get; set; }
        public byte NametableY { get; set; }
        public byte FineY { get; set; }
        public byte Unused { get; set; }
        public bool Latched { get; set; }
        public byte Register { get; set; }
    }

    public enum PpuScrollRegisterFlags
    {
        ScrollX,
        ScrollY,
        Latched
    }
}