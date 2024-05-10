using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solution.Solver.AtomGenerators.Output
{
    /// <summary>
    /// The part of the solution that assembles a product by bonding atoms together.
    /// </summary>
    public class AssemblyArea : SolverComponent
    {
        public List<Arm> LowerArms { get; private set; }
        public List<Arm> UpperArms { get; private set; }

        public int Width { get; private set; }
        public bool HasTriplex { get; private set; }

        public override Vector2 OutputPosition => new Vector2(2, 1);

        private List<Glyph> m_bonders = new List<Glyph>();
        private HashSet<Glyph> m_usedBonders = new HashSet<Glyph>();

        public AssemblyArea(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer, parent.OutputPosition)
        {
            Width = products.Max(p => p.Width);
            HasTriplex = products.Any(p => p.HasTriplex);

            CreateBonders();
            CreateArms();
            CreateTracks();
        }

        private void CreateBonders()
        {
            var position = new Vector2(0, 0);
            AddBonder(ref position, 0, 0, Direction.E, GlyphType.Bonding);
            AddBonder(ref position, Width + 2, 0, Direction.NE, GlyphType.Bonding);
            AddBonder(ref position, Width, 0, Direction.NW, GlyphType.Bonding);

            if (HasTriplex)
            {
                AddBonder(ref position, Width - 1, 0, Direction.E, GlyphType.TriplexBonding);
                AddBonder(ref position, 3, 0, Direction.NW, GlyphType.TriplexBonding);
                AddBonder(ref position, 1, 1, Direction.SW, GlyphType.TriplexBonding);

                AddBonder(ref position, Width - 1, -1, Direction.E, GlyphType.Unbonding);
                AddBonder(ref position, Width, 0, Direction.NE, GlyphType.Unbonding);
                AddBonder(ref position, Width, 0, Direction.NW, GlyphType.Unbonding);
            }
        }

        private void AddBonder(ref Vector2 position, int xOffset, int yOffset, int direction, GlyphType type)
        {
            position.X += xOffset;
            position.Y += yOffset;
            m_bonders.Add(new Glyph(this, position, direction, type));
        }

        private void CreateArms()
        {
            LowerArms = Enumerable.Range(0, Width).Select(x => new Arm(this, new Vector2(-Width + x + 1, -2), Direction.NE, MechanismType.Piston, 2)).ToList();
            UpperArms = Enumerable.Range(0, Width).Select(x => new Arm(this, new Vector2(x + 2, -1), Direction.NE, MechanismType.Piston, 2)).ToList();
        }

        private void CreateTracks()
        {
            int lowerTrackLength = Width * 4 - 1;
            if (HasTriplex)
            {
                lowerTrackLength += Width * 4 + 2;
            }

            new Track(this, new Vector2(-Width + 1, -2), Direction.E, lowerTrackLength);
            new Track(this, new Vector2(2, -1), Direction.E, lowerTrackLength - Width - 1);
        }

        public void SetUsedBonders(GlyphType type, int? direction, bool used)
        {
            var bonders = m_bonders.Where(b => b.Type == type && (!direction.HasValue || b.Rotation == direction.Value));
            if (!bonders.Any())
            {
                throw new ArgumentException(Invariant($"Can't find bonder of type {type} and direction {direction}."));
            }

            if (used)
            {
                m_usedBonders.UnionWith(bonders);
            }
        }

        public void OptimizeParts()
        {
            foreach (var bonder in m_bonders)
            {
                if (!m_usedBonders.Contains(bonder))
                {
                    bonder.Remove();
                }
            }
        }
    }
}
