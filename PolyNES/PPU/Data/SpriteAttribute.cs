namespace PolyNES.PPU.Data
{
    public struct SpriteAttribute
    {
        /// <summary>
        /// Selects foreground pallet for the sprite
        /// </summary>
        public bool PalletLowByte { get; set; }
        
        /// <summary>
        /// Selects foreground pallet for the sprite
        /// </summary>
        public bool PalletHighByte { get; set; }
        
        /// <summary>
        /// Unused by PPU
        /// </summary>
        public bool U2 { get; set; }
        
        /// <summary>
        /// Unused by PPU
        /// </summary>
        public bool U3 { get; set; }
        
        /// <summary>
        /// Unused by PPU
        /// </summary>
        public bool U4 { get; set; }
        
        /// <summary>
        /// Renders sprite Above / Below the background
        ///
        /// if true sprite will render on top of background,
        /// else sprite will render behind background.
        /// </summary>
        public bool BackgroundPosition { get; set; }
        
        /// <summary>
        /// Determines if the sprite should be
        /// flipped horizontally
        /// </summary>
        public bool FlipHorizontal { get; set; }
        
        /// <summary>
        /// Determines if the sprite should be
        /// flipped vertically
        /// </summary>
        public bool FlipVertically { get; set; }
    }
}