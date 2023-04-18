namespace PolyNES.Interfaces
{
    public interface ISystemManager
    {
        void PowerOn();
        void LoadRom(string romLocation);
    }
}