using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class AtomCollection
    {
        private List<Atom> m_atoms;

        public IEnumerable<Atom> Atoms => m_atoms;
        public Transform2D Transform;
        public Vector2 LocalOrigin;

        public AtomCollection()
        {
            m_atoms = new();
        }

        public AtomCollection(IEnumerable<Atom> atoms)
        {
            m_atoms = atoms.ToList();
        }

        public Atom GetAtom(Vector2 position)
        {
            return m_atoms.SingleOrDefault(a => a.Position == position);
        }

        public void AddAtom(Atom atom)
        {
            m_atoms.Add(atom);
        }

        public IEnumerable<(Atom, Vector2)> GetTransformedAtomPositions()
        {
            return Atoms.Select(a => (a, Transform.Apply(a.Position - LocalOrigin)));
        }
    }
}
