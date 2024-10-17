using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Input.Complex
{
    public class MoleculeDismantler
    {
        public Molecule Molecule { get; private set; }

        public class Operation
        {
            /// <summary>
            /// The atom to unbond from the rest of the molecule.
            /// </summary>
            public Atom Atom { get; init; }

            /// <summary>
            /// The atom to unbond this atom from, or null if it's the last atom in a bond chain.
            /// </summary>
            public Atom NextAtom { get; init; }

            /// <summary>
            /// The required rotation of the molecule when unbonding this atom from the next one, using the same coordinate
            /// system as ComplexDisassembler.
            /// </summary>
            public HexRotation MoleculeRotation { get; set; }

            /// <summary>
            /// The delta rotation required to unbond the next atom after this one.
            /// </summary>
            public HexRotation RotationToNext { get; set; }
        }

        private record class UnbondedAtom(Atom Atom, Atom Next);

        private List<Operation> m_operations;
        public IReadOnlyList<Operation> Operations => m_operations;

        public IEnumerable<Element> GetElementOrder() => m_operations.Select(op => op.Atom.Element);

        public MoleculeDismantler(Molecule molecule)
        {
            Molecule = molecule;

            GenerateOperations();
        }

        private void GenerateOperations()
        {
            var orderedAtoms = DetermineAtomOrder();
            var ops1 = BuildOperations(orderedAtoms);

            // If it's a single-chain molecule, try reversing the order
            if (Molecule.Atoms.All(a => a.BondCount <= 2) && Molecule.Atoms.Count(a => a.BondCount == 1) == 2)
            {
                var reverseOrderedAtoms = new List<UnbondedAtom>();
                for (int i = orderedAtoms.Count - 1; i >= 0; i--)
                {
                    var next = (i > 0) ? orderedAtoms[i - 1].Atom : null;
                    reverseOrderedAtoms.Add(new UnbondedAtom(orderedAtoms[i].Atom, next));
                }

                var ops2 = BuildOperations(reverseOrderedAtoms);

                // Choose the order that minimises the number of counterclockwise rotations, since those are most likely to cause collisions
                int c1 = ops1.Count(o => o.RotationToNext == HexRotation.R120);
                int c2 = ops2.Count(o => o.RotationToNext == HexRotation.R120);

                if (c1 < c2)
                {
                    m_operations = ops1;
                }
                else if (c1 > c2)
                {
                    m_operations = ops2;
                }
                else
                {
                    c1 = ops1.Count(o => o.RotationToNext == HexRotation.R60);
                    c2 = ops2.Count(o => o.RotationToNext == HexRotation.R60);

                    if (c1 < c2)
                    {
                        m_operations = ops1;
                    }
                    else
                    {
                        m_operations = ops2;
                    }
                }
            }
            else
            {
                m_operations = ops1;
            }
        }

        private List<Operation> BuildOperations(List<UnbondedAtom> orderedAtoms)
        {
            var ops = orderedAtoms.Select(a => new Operation { Atom = a.Atom, NextAtom = a.Next }).ToList();

            if (orderedAtoms.Count == 1)
            {
                // Degenerate case
                return ops;
            }

            var currentRotation = HexRotation.R0;
            for (int i = 0; i < orderedAtoms.Count - 1; i++)
            {
                var unbondedAtom = orderedAtoms[i];
                var atom = unbondedAtom.Atom;
                var nextAtom = unbondedAtom.Next;

                var bondDir = (nextAtom.Position - atom.Position).ToRotation().Value;

                ops[i].MoleculeRotation = -bondDir + ComplexDisassembler.UnbondingDirection;
            }

            // The rotation for the last operation is arbitrary since there'll be only one atom at that stage.
            // But we make it the same as the rotation for the second last operation so that no rotation will be
            // required between them.
            ops[orderedAtoms.Count - 1].MoleculeRotation = ops[orderedAtoms.Count - 2].MoleculeRotation;

            for (int i = 0; i < orderedAtoms.Count - 1; i++)
            {
                ops[i].RotationToNext = ops[i + 1].MoleculeRotation - ops[i].MoleculeRotation;
            }

            return ops;
        }

        private List<UnbondedAtom> DetermineAtomOrder()
        {
            // For now, only allow molecules with no branches or loops
            if (DoesMoleculeHaveBondCycles(Molecule))
            {
                throw new UnsupportedException("MoleculeDismantler currently only supports molecules with no bond cycles.");
            }

            var molecule = new AtomCollection(Molecule, new());
            var orderedAtoms = new List<UnbondedAtom>();

            while (molecule.Atoms.Count > 0)
            {
                // Get the next leaf atom
                var currentAtom = molecule.Atoms.Where(a => a.BondCount == 1).MaxBy(a => a.Position.X);

                // Process the whole atom chain
                while (currentAtom != null)
                {
                    var bondedAtoms = molecule.GetAdjacentBondedAtoms(currentAtom);
                    if (bondedAtoms.Count > 1)
                    {
                        // This atom is bonded to 2 or more atoms, so it's part of a new chain
                        break;
                    }

                    var nextAtom = molecule.GetAdjacentBondedAtoms(currentAtom).SingleOrDefault().Value;
                    molecule.RemoveAtom(currentAtom);

                    orderedAtoms.Add(new UnbondedAtom(currentAtom, nextAtom));
                    currentAtom = nextAtom;
                }
            }

            return orderedAtoms;
        }

        private static bool DoesMoleculeHaveBondCycles(Molecule molecule)
        {
            var seenAtoms = new HashSet<Atom>();

            bool CheckForCycle(Atom currentAtom, Atom parent)
            {
                seenAtoms.Add(currentAtom);
                foreach (var (_, bondedAtom) in molecule.GetAdjacentBondedAtoms(currentAtom.Position))
                {
                    if (!seenAtoms.Contains(bondedAtom))
                    {
                        if (CheckForCycle(bondedAtom, currentAtom))
                        {
                            return true;
                        }
                    }
                    else if (bondedAtom != parent)
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var atom in molecule.Atoms)
            {
                if (!seenAtoms.Contains(atom) && CheckForCycle(atom, null))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
