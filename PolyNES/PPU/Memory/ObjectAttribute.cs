namespace PolyNES.PPU.Memory;

public class ObjectAttribute
{
    /// <summary>
    /// Y Position of the sprite
    /// </summary>
    public byte Y { get; set; }
    /// <summary>
    /// X position of the sprite
    /// </summary>
    public byte X { get; set; }
    /// <summary>
    /// Pattern ID
    /// </summary>
    public byte Id { get; set; }
    /// <summary>
    /// Sprite Attribute
    /// </summary>
    public byte Attribute { get; set; }

    public void Reset()
    {
        Y = 0xFF;
        Id = 0xFF;
        Attribute = 0xFF;
        X = 0xFF;
    }

    public void Set(ObjectAttribute oa)
    {
        oa.Y = Y;
        oa.X = X;
        oa.Id = Id;
        oa.Attribute = Attribute;
    }
    
}