using System.Collections.Generic;

namespace OpusSolver.Solver
{
    public class GridState
    {
        private Dictionary<Vector2, Element?> m_atoms = new();
        private Dictionary<Vector2, GlyphType> m_glyphs = new();

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

        public void RegisterGlyph(Vector2 position, GlyphType glyphType, GameObject relativeToObj = null)
        {
            position = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            m_glyphs[position] = glyphType;
        }

        public Element? GetAtom(Vector2 position)
        {
            return m_atoms.TryGetValue(position, out var element) ? element : null;
        }

        public IEnumerable<Vector2> GetAllAtomPositions() => m_atoms.Keys;

        public GlyphType? GetGlyph(Vector2 position)
        {
            return m_glyphs.TryGetValue(position, out var glyphType) ? glyphType : null;
        }
    }
}
