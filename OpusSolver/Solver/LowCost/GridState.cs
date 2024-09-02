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

        public void RegisterMolecule(Vector2 localOrigin, Transform2D transform, IEnumerable<Atom> atoms, GameObject relativeToObj)
        {
            RegisterMoleculeElements(localOrigin, transform, atoms, relativeToObj, true);
        }

        public void UnregisterMolecule(Vector2 localOrigin, Transform2D transform, IEnumerable<Atom> atoms, GameObject relativeToObj)
        {
            RegisterMoleculeElements(localOrigin, transform, atoms, relativeToObj, false);
        }

        private void RegisterMoleculeElements(Vector2 localOrigin, Transform2D transform, IEnumerable<Atom> atoms, GameObject relativeToObj, bool register)
        {
            var objTransform = relativeToObj?.GetWorldTransform() ?? new Transform2D();
            transform = objTransform.Apply(transform);

            foreach (var atom in atoms)
            {
                var pos = transform.Apply(atom.Position - localOrigin);
                m_atoms[pos] = register ? atom.Element : null;
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
