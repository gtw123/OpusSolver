namespace OpusSolver
{
    /// <summary>
    /// Represents a glyph on the hex grid.
    /// </summary>
    public class Glyph : GameObject
    {
        public GlyphType Type { get; private set; }

        public Glyph(GameObject parent, Vector2 position, int rotation, GlyphType type)
            : base(parent, position, rotation)
        {
            Type = type;
        }
    }
}
