using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class AtomCollection
    {
        private List<Atom> m_atoms;

        public IEnumerable<Atom> Atoms => m_atoms;
        public Transform2D WorldTransform;

        public AtomCollection()
        {
            m_atoms = new();
        }

        public AtomCollection(Element element, Transform2D transform, GameObject relativeToObj = null)
        {
            var atom = new Atom(element, HexRotation.All.ToDictionary(r => r, r => BondType.None), new Vector2());
            m_atoms = [atom];

            WorldTransform = relativeToObj?.GetWorldTransform().Apply(transform) ?? transform;
        }

        public AtomCollection(IEnumerable<Atom> atoms)
        {
            m_atoms = atoms.ToList();
        }

        public Atom GetAtom(Vector2 localPosition)
        {
            return m_atoms.SingleOrDefault(a => a.Position == localPosition);
        }

        public void AddAtom(Atom atom)
        {
            m_atoms.Add(atom);
        }

        public IEnumerable<(Atom atom, Vector2 position)> GetTransformedAtomPositions()
        {
            return Atoms.Select(a => (a, WorldTransform.Apply(a.Position)));
        }

        public Atom GetAtomAtTransformedPosition(Vector2 position, GameObject relativeToObj = null)
        {
            var worldPos = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            return GetAtom(WorldTransform.Inverse().Apply(worldPos));
        }
    }
}
