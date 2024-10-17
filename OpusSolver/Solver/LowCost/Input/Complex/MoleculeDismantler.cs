﻿using System.Collections.Generic;
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
            /// The atom to unbond this atom from, or null if it's the first atom to be unbonded.
            /// </summary>
            public Atom ParentAtom { get; init; }

            /// <summary>
            /// The required rotation of the molecule when unbonding this atom from its parent, using the same coordinate
            /// system as ComplexDisassembler.
            /// </summary>
            public HexRotation MoleculeRotation { get; set; }

            /// <summary>
            /// The delta rotation required to unbond the next atom after this one.
            /// </summary>
            public HexRotation RotationToNext { get; set; }
        }

        private record class UnbondedAtom(Atom Atom, Atom Parent);

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
                    var parent = (i < orderedAtoms.Count - 1) ? orderedAtoms[i + 1].Atom : null;
                    reverseOrderedAtoms.Add(new UnbondedAtom(orderedAtoms[i].Atom, parent));
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
            var ops = orderedAtoms.Select(a => new Operation { Atom = a.Atom, ParentAtom = a.Parent }).ToList();

            if (orderedAtoms.Count == 1)
            {
                // Degenerate case
                return ops;
            }

            var currentRotation = HexRotation.R0;
            for (int i = 1; i < orderedAtoms.Count; i++)
            {
                var unbondedAtom = orderedAtoms[i];
                var atom = unbondedAtom.Atom;
                var parentAtom = unbondedAtom.Parent;

                var bondDir = (atom.Position - parentAtom.Position).ToRotation().Value;

                ops[i].MoleculeRotation = -bondDir + ComplexDisassembler.UnbondingDirection;
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

        private List<UnbondedAtom> DetermineAtomOrder()
        {
            // For now, only allow molecules with no branches or loops
            if (!(Molecule.Atoms.All(a => a.BondCount <= 2) && Molecule.Atoms.Count(a => a.BondCount == 1) == 2))
            {
                throw new UnsupportedException("MoleculeDismantler currently only supports molecules with a single atom chain.");
            }

            // Start with the atoms with the fewest bonds, then use X position as an arbitrary tie-breaker
            var firstAtom = Molecule.Atoms.GroupBy(a => a.BondCount).OrderBy(g => g.Key).First().MaxBy(a => a.Position.X);
            var seenAtoms = new HashSet<Atom> { firstAtom };

            var orderedAtoms = new List<UnbondedAtom>();
            var atomsToProcess = new Stack<UnbondedAtom>();
            atomsToProcess.Push(new UnbondedAtom(firstAtom, null));

            // Do a depth-first search so that we build entire molecule chains one at a time
            while (atomsToProcess.Count > 0)
            {
                var currentAtom = atomsToProcess.Pop();
                orderedAtoms.Add(currentAtom);

                foreach (var (_, bondedAtom) in Molecule.GetAdjacentBondedAtoms(currentAtom.Atom.Position))
                {
                    if (!seenAtoms.Contains(bondedAtom))
                    {
                        seenAtoms.Add(bondedAtom);
                        atomsToProcess.Push(new UnbondedAtom(bondedAtom, currentAtom.Atom));
                    }
                }
            }

            return orderedAtoms;
        }
    }
}