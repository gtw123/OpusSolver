using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output.Complex
{
    public class MoleculeBuilder
    {
        public Molecule Product { get; private set; }

        public class Operation
        {
            public Atom Atom { get; init; }

            /// <summary>
            /// The rotation of the molecule after bonding this atom to the previous one. Assumes the bonder
            /// is aligned with the X axis, the previous atom is at (0, 0) and this atom is at (1, 0).
            /// </summary>
            public HexRotation MoleculeRotationRelativeToBonder { get; set; }

            /// <summary>
            /// The delta rotation required to bond the next atom after this one.
            /// </summary>
            public HexRotation RotationToNext { get; set; }
        }

        private List<Operation> m_operations;
        public IReadOnlyList<Operation> Operations => m_operations;

        public IEnumerable<Element> GetElementsInBuildOrder() => m_operations.Select(op => op.Atom.Element);

        public MoleculeBuilder(Molecule product)
        {
            Product = product;

            GenerateOperations();
        }

        private void GenerateOperations()
        {
            var orderedAtoms = DetermineAtomOrder();
            var ops1 = BuildOperations(orderedAtoms);

            orderedAtoms.Reverse();
            var ops2 = BuildOperations(orderedAtoms);

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

        private List<Operation> BuildOperations(List<Atom> orderedAtoms)
        {
            var ops = orderedAtoms.Select(a => new Operation { Atom = a }).ToList();

            var currentRotation = HexRotation.R0;
            for (int i = 1; i < orderedAtoms.Count; i++)
            {
                var atom = orderedAtoms[i];
                var prevAtom = orderedAtoms[i - 1];
                var bondDir = (atom.Position - prevAtom.Position).ToRotation().Value;

                ops[i].MoleculeRotationRelativeToBonder = -bondDir;
            }

            for (int i = 1; i < orderedAtoms.Count - 1; i++)
            {
                ops[i].RotationToNext = ops[i + 1].MoleculeRotationRelativeToBonder - ops[i].MoleculeRotationRelativeToBonder;
            }

            return ops;
        }

        private List<Atom> DetermineAtomOrder()
        {
            var firstAtom = Product.Atoms.Where(a => a.BondCount == 1).MaxBy(a => a.Position.X);
            var seenAtoms = new HashSet<Atom> { firstAtom };

            var orderedAtoms = new List<Atom>();
            var atomsToProcess = new Queue<Atom>();
            atomsToProcess.Enqueue(firstAtom);

            while (atomsToProcess.Count > 0)
            {
                var currentAtom = atomsToProcess.Dequeue();
                orderedAtoms.Add(currentAtom);

                foreach (var (_, bondedAtom) in Product.GetAdjacentBondedAtoms(currentAtom.Position))
                {
                    if (!seenAtoms.Contains(bondedAtom))
                    {
                        seenAtoms.Add(bondedAtom);
                        atomsToProcess.Enqueue(bondedAtom);
                    }
                }
            }

            return orderedAtoms;
        }
    }
}
