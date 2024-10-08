using System.Collections.Generic;
using System;
using System.Linq;

namespace OpusSolver
{
    /// <summary>
    /// Represents a glyph on the hex grid.
    /// </summary>
    public class Glyph : GameObject
    {
        public GlyphType Type { get; private set; }

        public Glyph(GameObject parent, Vector2 position, HexRotation rotation, GlyphType type)
            : base(parent, position, rotation)
        {
            Type = type;
        }

        public IReadOnlyList<Vector2> GetCells()
        {
            var cells = new List<Vector2> { new(0, 0) };
            cells.AddRange(Type switch
            {
                GlyphType.Bonding => [new(1, 0)],
                GlyphType.MultiBonding => [new(1, 0), new(0, -1), new(-1, 1)],
                GlyphType.TriplexBonding => [new(1, 0), new(0, 1)],
                GlyphType.Unbonding => [new(1, 0)],
                GlyphType.Calcification => [],
                GlyphType.Duplication => [new(1, 0)],
                GlyphType.Projection => [new(1, 0)],
                GlyphType.Purification => [new(1, 0), new(0, 1)],
                GlyphType.Animismus => [new(1, 0), new(0, 1), new(1, -1)],
                GlyphType.Disposal => [new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1), new(1, -1)],
                GlyphType.Equilibrium => [],
                GlyphType.Unification => [new(0, 1), new(-1, 1), new(0, -1), new(1, -1)],
                GlyphType.Dispersion => [new(1, 0), new(1, -1), new(0, -1), new(-1, 0)],
                _ => throw new InvalidOperationException($"Unknown glyph type {Type}.")
            });

            return cells;
        }

        public IReadOnlyList<Vector2> GetWorldCells()
        {
            var worldTransform = GetWorldTransform();
            return GetCells().Select(c => worldTransform.Apply(c)).ToArray();
        }
    }
}
