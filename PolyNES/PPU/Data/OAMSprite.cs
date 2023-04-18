namespace PolyNES.PPU.Data
{
    public class OAMSprite
    {
        /// <summary>
        /// This byte controls the vertical / Y-Coordinate
        /// of the sprite
        /// </summary>
        public byte YCoordinate { get; set; }
        
        /// <summary>
        /// This byte specifies which tile to render from
        /// pattern tables.
        /// </summary>
        public byte PatternTableIndex { get; set; }
        
        /// <summary>
        /// Controls sprite attributes
        /// </summary>
        public SpriteAttribute Attributes { get; set; }
        
        /// <summary>
        /// This byte controls the Horizontal / X-Coordinate
        /// of the sprite
        /// </summary>
        public byte XCoordinate { get; set; }
    }
}