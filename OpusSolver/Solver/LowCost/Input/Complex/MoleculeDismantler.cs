using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Input.Complex
{
    public class MoleculeDismantler
    {
        public Molecule Molecule { get; private set; }
        private bool m_reverseElementOrder;
        private bool m_reverseBondTraversalDirection;

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

        /// <summary>
        /// The molecule with all non-essential bonds removed, so that the atoms are still all connected
        /// but there are no bond cycles.
        /// </summary>
        public AtomCollection BondReducedMolecule { get; private set; }

        public IEnumerable<Element> GetElementOrder() => m_operations.Select(op => op.Atom.Element);

        public MoleculeDismantler(Molecule molecule, bool reverseElementOrder, bool reverseBondTraversalDirection)
        {
            Molecule = molecule;
            m_reverseElementOrder = reverseElementOrder;
            m_reverseBondTraversalDirection = reverseBondTraversalDirection;

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
            var remainingAtoms = new AtomCollection(Molecule, new());
            BondReducedMolecule = new AtomCollection(Molecule, new());
            var orderedAtoms = new List<UnbondedAtom>();

            Atom GetNextLeafAtom()
            {
                var atoms = remainingAtoms.Atoms.Where(a => a.BondCount == 1);
                if (m_reverseElementOrder)
                {
                    atoms = atoms.OrderBy(a => a.Position.X).ThenBy(a => a.Position.Y);
                }
                else
                {
                    atoms = atoms.OrderByDescending(a => a.Position.X).ThenByDescending(a => a.Position.Y);
                }

                return atoms.FirstOrDefault();
            }

            while (remainingAtoms.Atoms.Count > 0)
            {
                // Get the next leaf atom
                var currentAtom = GetNextLeafAtom();
                while (currentAtom == null)
                {
                    // There are no leaf atoms, so try an atom with the fewest numbers of bonds (>= 2)
                    var nextAtoms = remainingAtoms.Atoms.OrderBy(a => a.BondCount).ThenBy(a => a.Position.X).ThenBy(a => a.Position.Y);

                    // Find the first atom which has an adjacent atom that is part of a cycle
                    var atomsInCycle = nextAtoms.Select(a => new { Atom = a, Adjacent = FindAdjacentAtomOnCycle(remainingAtoms, a) }).FirstOrDefault(a => a.Adjacent != null);
                    if (atomsInCycle == null)
                    {
                        // This should never happen...
                        throw new SolverException("Molecule contains no leaf nodes but also no cycles.");
                    }

                    // Remove the bond to break the cycle
                    remainingAtoms.RemoveBond(atomsInCycle.Atom.Position, atomsInCycle.Adjacent.Position);
                    BondReducedMolecule.RemoveBond(atomsInCycle.Atom.Position, atomsInCycle.Adjacent.Position);

                    currentAtom = GetNextLeafAtom();
                }

                // Process the whole atom chain
                while (currentAtom != null)
                {
                    var bondedAtoms = remainingAtoms.GetAdjacentBondedAtoms(currentAtom);
                    if (bondedAtoms.Count > 1)
                    {
                        // This atom is bonded to 2 or more atoms, so it's part of a new chain
                        break;
                    }

                    var nextAtom = remainingAtoms.GetAdjacentBondedAtoms(currentAtom).SingleOrDefault().Value;
                    remainingAtoms.RemoveAtom(currentAtom);

                    orderedAtoms.Add(new UnbondedAtom(currentAtom, nextAtom));
                    currentAtom = nextAtom;
                }
            }

            return orderedAtoms;
        }

        private Atom FindAdjacentAtomOnCycle(AtomCollection molecule, Atom startAtom)
        {
            var seenAtoms = new HashSet<Atom>();

            Atom FindAtomInCycle(Atom currentAtom, Atom parent)
            {
                seenAtoms.Add(currentAtom);
                foreach (var (_, bondedAtom) in molecule.GetAdjacentBondedAtoms(currentAtom).ConditionalReverse(m_reverseBondTraversalDirection))
                {
                    if (!seenAtoms.Contains(bondedAtom))
                    {
                        var cycleAtom = FindAtomInCycle(bondedAtom, currentAtom);
                        if (cycleAtom != null)
                        {
                            return cycleAtom;
                        }
                    }
                    else if (bondedAtom != parent && bondedAtom == startAtom)
                    {
                        return currentAtom;
                    }
                }

                return null;
            }

            return FindAtomInCycle(startAtom, null);
        }
    }
}
