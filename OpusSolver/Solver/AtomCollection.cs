using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class AtomCollection
    {
        private List<Atom> m_atoms;

        public IReadOnlyList<Atom> Atoms => m_atoms;
        public Transform2D WorldTransform;

        /// <summary>
        /// The molecule that these atoms are being assembled into (optional). Used to help determine
        /// which bonds are allowed to be created between the atoms in this collection.
        /// </summary>
        public AtomCollection TargetMolecule { get; set; }

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

        public AtomCollection Copy()
        {
            return new AtomCollection(m_atoms.Select(a => a.Copy()).ToList(), WorldTransform);
        }

        public Atom GetAtom(Vector2 localPosition)
        {
            return m_atoms.SingleOrDefault(a => a.Position == localPosition);
        }

        public void AddAtom(Atom atom)
        {
            m_atoms.Add(atom);
        }

        /// <summary>
        /// Removes an atom (and its bonds) from the collection. Returns a new collection containing the removed atom.
        /// </summary>
        public AtomCollection RemoveAtom(Atom atom)
        {
            if (!m_atoms.Remove(atom))
            {
                throw new ArgumentException("Cannot remove an atom that is not part of this AtomCollection.");
            }

            foreach (var dir in HexRotation.All)
            {
                var otherAtom = GetAtom(atom.Position.OffsetInDirection(dir, 1));
                if (otherAtom != null && atom.Bonds[dir] != BondType.None)
                {
                    otherAtom.Bonds[dir + HexRotation.R180] = BondType.None;
                }
            }

            return new AtomCollection(atom.Element, new Transform2D(WorldTransform.Apply(atom.Position), HexRotation.R0));
        }

        public void AddBond(Vector2 atom1Pos, Vector2 atom2Pos)
        {
            var atom1 = GetAtom(atom1Pos) ?? throw new ArgumentException($"No atom found at {atom1Pos}.");
            var atom2 = GetAtom(atom2Pos) ?? throw new ArgumentException($"No atom found at {atom2Pos}.");

            if (atom1Pos.DistanceBetween(atom2Pos) != 1)
            {
                throw new ArgumentException($"Can't create bonds between non-adjacent atoms {atom1Pos} and {atom2Pos}.");
            }

            var bondDir1 = (atom2Pos - atom1Pos).ToRotation() ?? throw new InvalidOperationException($"Can't determine bond direction.");
            if (atom1.Bonds[bondDir1] != BondType.None)
            {
                throw new InvalidOperationException($"Atom at {atom1Pos} already has a bond to {atom2Pos}.");
            }

            var bondDir2 = bondDir1 + HexRotation.R180;
            if (atom2.Bonds[bondDir2] != BondType.None)
            {
                throw new InvalidOperationException($"Atom at {atom2Pos} already has a bond to {atom1Pos}.");
            }

            atom1.Bonds[bondDir1] = BondType.Single;
            atom2.Bonds[bondDir2] = BondType.Single;
        }

        public void RemoveBond(Vector2 atom1Pos, Vector2 atom2Pos)
        {
            var atom1 = GetAtom(atom1Pos) ?? throw new ArgumentException($"No atom found at {atom1Pos}.");
            var atom2 = GetAtom(atom2Pos) ?? throw new ArgumentException($"No atom found at {atom2Pos}.");

            if (atom1Pos.DistanceBetween(atom2Pos) != 1)
            {
                throw new ArgumentException($"Can't remove bonds between non-adjacent atoms {atom1Pos} and {atom2Pos}.");
            }

            var bondDir1 = (atom2Pos - atom1Pos).ToRotation() ?? throw new InvalidOperationException($"Can't determine bond direction.");
            if (atom1.Bonds[bondDir1] == BondType.None)
            {
                throw new InvalidOperationException($"Atom at {atom1Pos} doesn't already have a bond to {atom2Pos}.");
            }

            var bondDir2 = bondDir1 + HexRotation.R180;
            if (atom2.Bonds[bondDir2] == BondType.None)
            {
                throw new InvalidOperationException($"Atom at {atom2Pos} doesn't already have a bond to {atom1Pos}.");
            }

            atom1.Bonds[bondDir1] = BondType.None;
            atom2.Bonds[bondDir2] = BondType.None;
        }

        public IEnumerable<(Atom atom, Vector2 position)> GetWorldAtomPositions()
        {
            return GetTransformedAtomPositions(WorldTransform);
        }

        public IEnumerable<(Atom atom, Vector2 position)> GetTransformedAtomPositions(Transform2D transform, GameObject relativeToObj = null)
        {
            transform = relativeToObj?.GetWorldTransform().Apply(transform) ?? transform;
            return Atoms.Select(a => (a, transform.Apply(a.Position)));
        }

        public Atom GetAtomAtWorldPosition(Vector2 position, GameObject relativeToObj = null)
        {
            var worldPos = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            return GetAtom(WorldTransform.Inverse().Apply(worldPos));
        }

        public Atom GetAtomAtTransformedPosition(Transform2D transform, Vector2 position, GameObject relativeToObj = null)
        {
            var worldPos = relativeToObj?.GetWorldTransform().Apply(position) ?? position;
            return GetAtom(transform.Inverse().Apply(worldPos));
        }
    }
}
