using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class GridState
    {
        private Dictionary<Vector2, Element?> m_atoms = new();
        private Dictionary<Vector2, Glyph> m_glyphs = new();
        private Dictionary<Vector2, Reagent> m_reagents = new();
        private Dictionary<Vector2, Track> m_tracks = new();

        public void RegisterAtom(Vector2 position, Element? element, GameObject relativeToObj)
        {
            position = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            m_atoms[position] = element;
        }

        public void UnregisterAtom(Vector2 position, GameObject relativeToObj)
        {
            position = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            m_atoms.Remove(position);
        }

        public void RegisterAtoms(AtomCollection atomCollection)
        {
            foreach (var (atom, pos) in atomCollection.GetWorldAtomPositions())
            {
                m_atoms[pos] = atom.Element;
            }
        }

        public void UnregisterAtoms(AtomCollection atomCollection)
        {
            foreach (var (_, pos) in atomCollection.GetWorldAtomPositions())
            {
                m_atoms.Remove(pos);
            }
        }

        public void RegisterGlyph(Glyph glyph)
        {
            var glyphType = glyph.Type;

            var cells = new List<Vector2> { new(0, 0) };
            cells.AddRange(glyphType switch
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
                _ => throw new InvalidOperationException($"Unknown glyph type {glyphType}.")
            });

            var transform = glyph.GetWorldTransform();
            foreach (var pos in cells.Select(c => transform.Apply(c)))
            {
                m_glyphs[pos] = glyph;
            }
        }

        public void RegisterReagent(Reagent reagent)
        {
            var transform = reagent.GetWorldTransform();
            foreach (var pos in reagent.Molecule.Atoms.Select(a => transform.Apply(a.Position)))
            {
                m_reagents[pos] = reagent;
            }
        }

        public void RegisterTrack(Track track)
        {
            foreach (var pos in track.GetAllPathCells())
            {
                m_tracks[pos] = track;
            }
        }

        public Element? GetAtom(Vector2 position)
        {
            return m_atoms.TryGetValue(position, out var element) ? element : null;
        }

        public IEnumerable<Vector2> GetAllAtomPositions() => m_atoms.Keys;

        public Glyph GetGlyph(Vector2 position)
        {
            return m_glyphs.TryGetValue(position, out var glyph) ? glyph : null;
        }
    }
}
