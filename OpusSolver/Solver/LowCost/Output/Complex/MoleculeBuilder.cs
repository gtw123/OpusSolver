using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output.Complex
{
    public class MoleculeBuilder
    {
        public Molecule Product { get; private set; }
        private bool m_reverseElementOrder;
        private bool m_useBreadthFirstSearch;
        private bool m_reverseBondTraversalDirection;

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

        public MoleculeBuilder(Molecule product, bool reverseElementOrder, bool useBreadthFirstSearch, bool reverseBondTraversalDirection)
        {
            Product = product;
            m_reverseElementOrder = reverseElementOrder;
            m_useBreadthFirstSearch = useBreadthFirstSearch;
            m_reverseBondTraversalDirection = reverseBondTraversalDirection;

            GenerateOperations();
        }

        private void GenerateOperations()
        {
            var orderedAtoms = DetermineAtomOrder();
            m_operations = BuildOperations(orderedAtoms);
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

        private enum ContainerType { Stack, Queue };

        private class StackOrQueue<T>
        {
            private interface IContainerWrapper
            {
                public void Add(T item);
                public T RemoveNext();
                public bool IsEmpty { get; }
            }

            private class StackWrapper : IContainerWrapper
            {
                private Stack<T> m_stack = new();

                public void Add(T item) => m_stack.Push(item);
                public T RemoveNext() => m_stack.Pop();
                public bool IsEmpty => m_stack.Count == 0;
            }

            private class QueueWrapper : IContainerWrapper
            {
                private Queue<T> m_queue = new();

                public void Add(T item) => m_queue.Enqueue(item);
                public T RemoveNext() => m_queue.Dequeue();
                public bool IsEmpty => m_queue.Count == 0;
            }

            private IContainerWrapper m_container;

            public StackOrQueue(ContainerType type)
            {
                m_container = type == ContainerType.Stack ? new StackWrapper() : new QueueWrapper();
            }

            public void Add(T item) => m_container.Add(item);
            public T RemoveNext() => m_container.RemoveNext();
            public bool IsEmpty => m_container.IsEmpty;
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

            // By default we use depth-first search (stack) so that we build entire molecule chains one at a time.
            // But for some molecules it's better to use a breadth-first search (queue).
            var atomsToProcess = new StackOrQueue<BondedAtom>(m_useBreadthFirstSearch ? ContainerType.Queue : ContainerType.Stack);
            atomsToProcess.Add(new BondedAtom(firstAtom, null));

            while (!atomsToProcess.IsEmpty)
            {
                var currentAtom = atomsToProcess.RemoveNext();
                orderedAtoms.Add(currentAtom);

                foreach (var (_, bondedAtom) in Product.GetAdjacentBondedAtoms(currentAtom.Atom.Position).ConditionalReverse(m_reverseBondTraversalDirection))
                {
                    if (!seenAtoms.Contains(bondedAtom))
                    {
                        seenAtoms.Add(bondedAtom);
                        atomsToProcess.Add(new BondedAtom(bondedAtom, currentAtom.Atom));
                    }
                }
            }

            return orderedAtoms;
        }
    }
}
