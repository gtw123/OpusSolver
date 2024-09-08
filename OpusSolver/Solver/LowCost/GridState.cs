using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
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

        public void RegisterAtoms(AtomCollection atomCollection, GameObject relativeToObj)
        {
            RegisterMoleculeElements(atomCollection, relativeToObj, true);
        }

        public void UnregisterAtoms(AtomCollection atomCollection, GameObject relativeToObj)
        {
            RegisterMoleculeElements(atomCollection, relativeToObj, false);
        }

        private void RegisterMoleculeElements(AtomCollection atomCollection, GameObject relativeToObj, bool register)
        {
            var objTransform = relativeToObj?.GetWorldTransform() ?? new Transform2D();

            foreach (var (atom, pos) in atomCollection.GetTransformedAtomPositions())
            {
                var worldPos = objTransform.Apply(pos);
                m_atoms[worldPos] = register ? atom.Element : null;
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

        public GlyphType? GetGlyph(Vector2 position)
        {
            return m_glyphs.TryGetValue(position, out var glyphType) ? glyphType : null;
        }
    }
}
