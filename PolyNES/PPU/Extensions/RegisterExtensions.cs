namespace PolyNES.PPU.Extensions
{
    public static class RegisterExtensions
    {
        public static byte AsByte(this bool value)
        {
            return value ? (byte) 1 : (byte) 0;
        }
    }
}