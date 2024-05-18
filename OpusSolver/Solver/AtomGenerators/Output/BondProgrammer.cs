using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// Generates instructions for bonding a row of atoms to the previous row, and adding triplex bonds.
    /// </summary>
    public class BondProgrammer
    {
        public UniversalMoleculeAssembler Assembler { get; private set; }
        public Molecule Molecule { get; private set; }
        public int Row { get; private set; }

        private List<Instruction> m_instructions;

        public BondProgrammer(UniversalMoleculeAssembler assembler, Molecule molecule, int row)
        {
            Assembler = assembler;
            Molecule = molecule;
            Row = row;
        }

        public (IEnumerable<Instruction> bondInstructions, IEnumerable<Instruction> returnInstructions) Generate()
        {
            m_instructions = new List<Instruction>();

            AddBonds();
            Optimize();

            return (m_instructions, GenerateReturnInstructions());
        }

        private void AddBonds()
        {
            // Do single bonds
            MoveThroughBonder(GlyphType.Bonding, Direction.NE, Assembler.Width, a => a.Bonds[Direction.NE] == BondType.Single);
            MoveThroughBonder(GlyphType.Bonding, Direction.NW, Assembler.Width - 1, a => a.Bonds[Direction.NW] == BondType.Single);

            if (HasTriplexBonds())
            {
                // Move the product through all the triplex bonders
                Add(Instruction.MovePositive, Instruction.Retract);
                Repeat(Instruction.MovePositive, Assembler.Width + 3);
                Add(Instruction.Extend);
                Assembler.SetUsedBonders(GlyphType.TriplexBonding, null, true);

                // Now remove any extra bonds created between fire atoms
                MoveThroughBonder(GlyphType.Unbonding, Direction.E, Assembler.Width - 1, a => IsUnbondedFirePair(a, Direction.W));
                MoveThroughBonder(GlyphType.Unbonding, Direction.NE, Assembler.Width, a => IsUnbondedFirePair(a, Direction.NE));
                MoveThroughBonder(GlyphType.Unbonding, Direction.NW, Assembler.Width - 1, a => IsUnbondedFirePair(a, Direction.NW));
            }
        }

        private bool HasTriplexBonds()
        {
            var atoms = Molecule.GetRow(Row);
            return atoms.Any(a => a.Bonds[Direction.E] == BondType.Triplex || a.Bonds[Direction.NE] == BondType.Triplex || a.Bonds[Direction.NW] == BondType.Triplex);
        }

        /// <summary>
        /// Returns whether an atom and the atom in the specified direction are both fire atoms and have no bond between them.
        /// </summary>
        private bool IsUnbondedFirePair(Atom atom, int direction)
        {
            return atom.Element == Element.Fire
                && atom.Bonds[direction] == BondType.None
                && Molecule.GetAdjacentAtom(atom.Position, direction)?.Element == Element.Fire;
        }

        private void MoveThroughBonder(GlyphType type, int direction, int count, Func<Atom, bool> shouldBondAtom)
        {
            for (int i = 0; i < count; i++)
            {
                Add(Instruction.MovePositive);
                var atom = Molecule.GetAtom(new Vector2(Assembler.Width - 1 - i, Row));
                if (atom != null && shouldBondAtom(atom))
                {
                    Add(Instruction.Retract, Instruction.Extend);
                    Assembler.SetUsedBonders(type, direction, true);
                }
            }
        }

        private void Optimize()
        {
            // Remove trailing MovePositive instructions
            // Note that if all instructions are MovePositive, FindLastIndex conveniently returns -1 which
            // makes lastIndex equal 0 and so we remove all instructions.
            int lastIndex = m_instructions.FindLastIndex(i => i != Instruction.MovePositive) + 1;
            if (lastIndex < m_instructions.Count)
            {
                m_instructions.RemoveRange(lastIndex, m_instructions.Count - lastIndex);
            }

            // Replace Extend/MovePositive/Retract with just MovePositive
            for (int i = 0; i < m_instructions.Count - 2; i++)
            {
                if (m_instructions[i] == Instruction.Extend
                    && m_instructions[i + 1] == Instruction.MovePositive
                    && m_instructions[i + 2] == Instruction.Retract)
                {
                    m_instructions[i] = Instruction.MovePositive;
                    m_instructions.RemoveRange(i + 1, 2);
                }
            }
        }

        private IEnumerable<Instruction> GenerateReturnInstructions()
        {
            int distance = m_instructions.Count(i => i == Instruction.MovePositive);
            return Enumerable.Repeat(Instruction.MoveNegative, distance);
        }

        private void Add(params Instruction[] instructions)
        {
            m_instructions.AddRange(instructions);
        }

        private void Repeat(Instruction instruction, int count)
        {
            m_instructions.AddRange(Enumerable.Repeat(instruction, count));
        }
    }
}
