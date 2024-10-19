using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output.Complex
{
    public class MoleculeBuilder
    {
        public Molecule Product { get; private set; }
        private bool m_reverseElementOrder;

        public class Operation
        {
            /// <summary>
            /// The atom to bond to the rest of the molecule.
            /// </summary>
            public Atom Atom { get; init; }

            /// <summary>
            /// The existing atom to bond this atom to, or null if it's the first atom to be assembled.
            /// </summary>
            public Atom ParentAtom { get; init; }

            /// <summary>
            /// The required rotation of the molecule when bonding this atom to its parent, using the same coordinate
            /// system as ComplexAssembler.
            /// </summary>
            public HexRotation MoleculeRotation { get; set; }

            /// <summary>
            /// The delta rotation required to bond the next atom after this one.
            /// </summary>
            public HexRotation RotationToNext { get; set; }
        }

        private record class BondedAtom(Atom Atom, Atom Parent);

        private List<Operation> m_operations;
        public IReadOnlyList<Operation> Operations => m_operations;

        public IEnumerable<Element> GetElementsInBuildOrder() => m_operations.Select(op => op.Atom.Element);

        public MoleculeBuilder(Molecule product, bool reverseElementOrder)
        {
            Product = product;
            m_reverseElementOrder = reverseElementOrder;

            GenerateOperations();
        }

        private void GenerateOperations()
        {
            var orderedAtoms = DetermineAtomOrder();
            var ops1 = BuildOperations(orderedAtoms);

            // If it's a single-chain molecule, try reversing the order
            if (Product.Atoms.All(a => a.BondCount <= 2) && Product.Atoms.Count(a => a.BondCount == 1) == 2)
            {
                var reverseOrderedAtoms = new List<BondedAtom>();
                for (int i = orderedAtoms.Count - 1; i >= 0; i--)
                {
                    var parent = (i < orderedAtoms.Count - 1) ? orderedAtoms[i + 1].Atom : null;
                    reverseOrderedAtoms.Add(new BondedAtom(orderedAtoms[i].Atom, parent));
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

        private List<Operation> BuildOperations(List<BondedAtom> orderedAtoms)
        {
            var ops = orderedAtoms.Select(a => new Operation { Atom = a.Atom, ParentAtom = a.Parent }).ToList();

            if (orderedAtoms.Count == 1)
            {
                // Degenerate case
                return ops;
            }

            var currentRotation = HexRotation.R0;
            for (int i = 1; i < orderedAtoms.Count; i++)
            {
                var bondedAtom = orderedAtoms[i];
                var atom = bondedAtom.Atom;
                var parentAtom = bondedAtom.Parent;

                var bondDir = (atom.Position - parentAtom.Position).ToRotation().Value;

                ops[i].MoleculeRotation = -bondDir + ComplexAssembler.BondingDirection;
            }

            // The rotation for the first operation is arbitrary since there'll be only one atom at that stage.
            // But we make it the same as the rotation for the second operation so that no rotation will be
            // required between them.
            ops[0].MoleculeRotation = ops[1].MoleculeRotation;

            for (int i = 0; i < orderedAtoms.Count - 1; i++)
            {
                ops[i].RotationToNext = ops[i + 1].MoleculeRotation - ops[i].MoleculeRotation;
            }

            return ops;
        }

        private List<BondedAtom> DetermineAtomOrder()
        {
            // Start with the atom with the fewest bonds, then use X and Y positions as arbitrary tie-breakers
            var atoms = Product.Atoms.OrderBy(a => a.BondCount);
            if (m_reverseElementOrder)
            {
                atoms = atoms.ThenBy(a => a.Position.X).ThenBy(a => a.Position.Y);
            }
            else
            {
                atoms = atoms.ThenByDescending(a => a.Position.X).ThenByDescending(a => a.Position.Y);
            }

            var firstAtom = atoms.First();
            var seenAtoms = new HashSet<Atom> { firstAtom };

            var orderedAtoms = new List<BondedAtom>();
            var atomsToProcess = new Stack<BondedAtom>();
            atomsToProcess.Push(new BondedAtom(firstAtom, null));

            // Do a depth-first search so that we build entire molecule chains one at a time
            while (atomsToProcess.Count > 0)
            {
                var currentAtom = atomsToProcess.Pop();
                orderedAtoms.Add(currentAtom);

                foreach (var (_, bondedAtom) in Product.GetAdjacentBondedAtoms(currentAtom.Atom.Position))
                {
                    if (!seenAtoms.Contains(bondedAtom))
                    {
                        seenAtoms.Add(bondedAtom);
                        atomsToProcess.Push(new BondedAtom(bondedAtom, currentAtom.Atom));
                    }
                }
            }

            return orderedAtoms;
        }
    }
}
