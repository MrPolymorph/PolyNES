namespace PolyNES.PPU.Registers
{
    /// <summary>
    /// The high 5 bits of the register store the vertical scroll offset, and the low 3 bits store the horizontal scroll
    /// offset. By updating these values, the PPU can render different parts of the nametable on the screen.
    /// </summary>
    public class PpuScrollRegister
    {
        /// <summary>
        /// The high 5 bits of the register store the vertical scroll offset
        /// </summary>
        public byte ScrollY { get; set; }
        
        /// <summary>
        /// the low 3 bits store the horizontal scroll offset
        /// </summary>
        public byte ScrollX { get; set; }

        public bool Latched { get; set; }

        public PpuScrollRegister()
        {
            
        }

        public void SetFlag(byte b)
        {
            ScrollY = (byte) (b & 0xF8);
            ScrollY = (byte) (b & 0x07);
        }
        
        public byte Register => (byte) (ScrollY + ScrollY);
    }

    public enum PpuScrollRegisterFlags
    {
        ScrollX,
        ScrollY,
        Latched
    }
}