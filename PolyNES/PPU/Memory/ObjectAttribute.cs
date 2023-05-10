namespace PolyNES.PPU.Memory;

public class ObjectAttribute
{
    public byte Y { get; set; }			// Y position of sprite
    public byte Id { get; set; }			// ID of tile from pattern memory
    public byte Attribute { get; set; }// Flags define how sprite should be rendered
    public byte X { get; set; }		// X position of sprite
}