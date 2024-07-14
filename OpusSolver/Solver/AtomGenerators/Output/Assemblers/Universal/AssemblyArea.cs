using OpusSolver.Solver.AtomGenerators.Output.Assemblers.Hex3;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers.Universal
{
    public class AssemblyArea : SolverComponent
    {
        public override Vector2 OutputPosition => new Vector2();

        public int Width { get; private set; }

        public bool HasSingle60Bonder { get; private set; }
        public bool HasSingle120Bonder { get; private set; }
        public bool HasTriplexBonders { get; private set; }

        public IReadOnlyList<Arm> LowerArms { get; private set; }
        public IReadOnlyList<Arm> UpperArms { get; private set; }

        private readonly List<Glyph> m_bonders = new();
        public IReadOnlyList<Glyph> Bonders => m_bonders;

        private readonly HashSet<Glyph> m_usedBonders = new HashSet<Glyph>();

        public AssemblyArea(SolverComponent parent, ProgramWriter writer, int width, IEnumerable<Molecule> products)
            : base(parent, writer, new Vector2(0, 0))
        {
            Width = width;

            HasSingle60Bonder = products.Any(p => p.Atoms.Any(a => a.Bonds[HexRotation.R60] == BondType.Single));
            HasSingle120Bonder = products.Any(p => p.Atoms.Any(a => a.Bonds[HexRotation.R120] == BondType.Single));
            HasTriplexBonders = products.Any(p => p.HasTriplex);

            CreateArms();
            CreateBonders();
            CreateTracks();
        }

        private void CreateArms()
        {
            LowerArms = Enumerable.Range(0, Width).Select(x => new Arm(this, new Vector2(-Width + x + 1, -2), HexRotation.R60, ArmType.Piston, 2)).ToList();
            UpperArms = Enumerable.Range(0, Width).Select(x => new Arm(this, new Vector2(x + 2, -1), HexRotation.R60, ArmType.Piston, 2)).ToList();
        }

        private void CreateBonders()
        {
            var position = new Vector2(0, 0);
            AddBonder(ref position, 0, 0, HexRotation.R0, GlyphType.Bonding);

            position += new Vector2(2, 0);

            if (HasSingle60Bonder)
            {
                AddBonder(ref position, Width, 0, HexRotation.R60, GlyphType.Bonding);
            }
            else
            {
                position += new Vector2(1, 0);
            }

            if (HasSingle120Bonder)
            {
                AddBonder(ref position, Width, 0, HexRotation.R120, GlyphType.Bonding);
            }
            else
            {
                position += new Vector2(1, 0);
            }

            if (HasTriplexBonders)
            {
                AddBonder(ref position, Width - 1, 0, HexRotation.R0, GlyphType.TriplexBonding);
                AddBonder(ref position, 3, 0, HexRotation.R120, GlyphType.TriplexBonding);
                AddBonder(ref position, 1, 1, HexRotation.R240, GlyphType.TriplexBonding);

                AddBonder(ref position, Width - 1, -1, HexRotation.R0, GlyphType.Unbonding);
                AddBonder(ref position, Width, 0, HexRotation.R60, GlyphType.Unbonding);
                AddBonder(ref position, Width, 0, HexRotation.R120, GlyphType.Unbonding);
            }
        }

        private void CreateTracks()
        {
            int lowerTrackLength = Width * 4 - 1;
            if (HasTriplexBonders)
            {
                lowerTrackLength += Width * 4 + 2;
            }

            new Track(this, new Vector2(-Width + 1, -2), HexRotation.R0, lowerTrackLength);
            new Track(this, new Vector2(2, -1), HexRotation.R0, lowerTrackLength - Width - 1);
        }

        private void AddBonder(ref Vector2 position, int xOffset, int yOffset, HexRotation direction, GlyphType type)
        {
            position.X += xOffset;
            position.Y += yOffset;
            m_bonders.Add(new Glyph(this, position, direction, type));
        }

        public void SetUsedBonders(GlyphType type, HexRotation? direction)
        {
            var bonders = m_bonders.Where(b => b.Type == type && (!direction.HasValue || b.Transform.Rotation == direction.Value));
            if (!bonders.Any())
            {
                throw new ArgumentException(FormattableString.Invariant($"Can't find bonder of type {type} and direction {direction}."));
            }

            m_usedBonders.UnionWith(bonders);
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
