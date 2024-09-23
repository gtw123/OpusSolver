using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class AtomCollection
    {
        private List<Atom> m_atoms;

        public IReadOnlyList<Atom> Atoms => m_atoms;
        public Transform2D WorldTransform;

        public AtomCollection()
        {
            m_atoms = new();
        }

        public AtomCollection(Element element, Transform2D transform, GameObject relativeToObj = null)
            : this([new Atom(element, HexRotation.All.ToDictionary(r => r, r => BondType.None), new Vector2())], transform, relativeToObj)
        {
        }

        public AtomCollection(Molecule molecule, Transform2D transform, GameObject relativeToObj = null)
            : this(molecule.Atoms.Select(a => a.Copy()).ToList(), transform, relativeToObj)
        {
        }

        private AtomCollection(List<Atom> copiedAtoms, Transform2D transform, GameObject relativeToObj = null)
        {
            m_atoms = copiedAtoms;
            WorldTransform = relativeToObj?.GetWorldTransform().Apply(transform) ?? transform;
        }

        public Atom GetAtom(Vector2 localPosition)
        {
            return m_atoms.SingleOrDefault(a => a.Position == localPosition);
        }

        public void AddAtom(Atom atom)
        {
            m_atoms.Add(atom);
        }

        public Atom RemoveAtom(int index)
        {
            var atom = m_atoms[index];
            m_atoms.RemoveAt(index);

            return atom;
        }

        public IEnumerable<(Atom atom, Vector2 position)> GetWorldAtomPositions()
        {
            return GetTransformedAtomPositions(WorldTransform);
        }

        public IEnumerable<(Atom atom, Vector2 position)> GetTransformedAtomPositions(Transform2D transform)
        {
            return Atoms.Select(a => (a, transform.Apply(a.Position)));
        }

        public Atom GetAtomAtTransformedPosition(Vector2 position, GameObject relativeToObj = null)
        {
            var worldPos = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            return GetAtom(WorldTransform.Inverse().Apply(worldPos));
        }
    }
}
