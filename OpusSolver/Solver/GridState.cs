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

        public void RegisterMolecule(AtomCollection molecule)
        {
            foreach (var (atom, pos) in molecule.GetWorldAtomPositions())
            {
                m_atoms[pos] = atom.Element;
            }
        }

        public void UnregisterMolecule(AtomCollection molecule)
        {
            foreach (var (_, pos) in molecule.GetWorldAtomPositions())
            {
                m_atoms.Remove(pos);
            }
        }

        public void RegisterGlyph(Glyph glyph)
        {
            foreach (var pos in glyph.GetWorldCells())
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

        /// <summary>
        /// Checks whether the specified grid cell contains a game object such as a glyph, reagent or track.
        /// Ignores atoms.
        public bool CellContainsAnyObject(Vector2 position)
        {
            return m_glyphs.ContainsKey(position) || m_reagents.ContainsKey(position) || m_tracks.ContainsKey(position);
        }
    }
}
