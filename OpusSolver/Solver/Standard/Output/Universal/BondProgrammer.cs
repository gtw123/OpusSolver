using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard.Output.Universal
{
    /// <summary>
    /// Generates instructions for bonding a row of atoms to the previous row, and adding triplex bonds.
    /// </summary>
    public class BondProgrammer
    {
        public Molecule Molecule { get; private set; }
        public int Row { get; private set; }

        public IEnumerable<Instruction> Instructions => m_instructions;
        public IEnumerable<Instruction> ReturnInstructions { get; private set; }

        private readonly AssemblyArea m_assemblyArea;
        private List<Instruction> m_instructions;

        public BondProgrammer(AssemblyArea assemblyArea, Molecule molecule, int row)
        {
            m_assemblyArea = assemblyArea;
            Molecule = molecule;
            Row = row;
        }

        public void Generate()
        {
            if (m_instructions != null)
            {
                throw new InvalidOperationException("Can't call Generate more than once on the same BondProgrammer.");
            }

            m_instructions = new List<Instruction>();

            AddBonds();
            Optimize();

            ReturnInstructions = GenerateReturnInstructions();
        }

        private void AddBonds()
        {
            if (m_assemblyArea.HasSingle60Bonder)
            {
                MoveThroughBonder(GlyphType.Bonding, HexRotation.R60, m_assemblyArea.Width, a => a.Bonds[HexRotation.R60] == BondType.Single);
            }
            else
            {
                Add(Instruction.MovePositive);
            }

            if (m_assemblyArea.HasSingle120Bonder)
            {
                MoveThroughBonder(GlyphType.Bonding, HexRotation.R120, m_assemblyArea.Width - 1, a => a.Bonds[HexRotation.R120] == BondType.Single);
            }

            int? firstTriplexPosition = GetLeftmostTriplexBondPositionInCurrentRow();
            if (firstTriplexPosition != null)
            {
                // Move the product through all the triplex bonders
                Add(Instruction.MovePositive, Instruction.Retract);

                // We need to move the molecule by maxMoves to get past all the triplex bonders, but we can 
                // extend early once we've made all the triplex bonds we need to make.
                int maxMoves = m_assemblyArea.Width + 3;
                Repeat(Instruction.MovePositive, maxMoves - firstTriplexPosition.Value);
                Add(Instruction.Extend);
                m_assemblyArea.SetUsedBonders(GlyphType.TriplexBonding, null);

                Repeat(Instruction.MovePositive, firstTriplexPosition.Value);
                
                // Now remove any extra bonds created between fire atoms
                MoveThroughBonder(GlyphType.Unbonding, HexRotation.R0, m_assemblyArea.Width - 1, a => IsUnbondedFirePair(a, HexRotation.R180));
                MoveThroughBonder(GlyphType.Unbonding, HexRotation.R60, m_assemblyArea.Width, a => IsUnbondedFirePair(a, HexRotation.R60));
                MoveThroughBonder(GlyphType.Unbonding, HexRotation.R120, m_assemblyArea.Width - 1, a => IsUnbondedFirePair(a, HexRotation.R120));
            }
        }

        private int? GetLeftmostTriplexBondPositionInCurrentRow()
        {
            var atoms = Molecule.GetRow(Row);

            int? pos0 = atoms.FirstOrDefault(atom => atom.Bonds[HexRotation.R0] == BondType.Triplex)?.Position.X;
            int? pos60 = atoms.FirstOrDefault(atom => atom.Bonds[HexRotation.R60] == BondType.Triplex)?.Position.X;
            int? pos120 = atoms.FirstOrDefault(atom => atom.Bonds[HexRotation.R120] == BondType.Triplex)?.Position.X - 1;

            return MathUtils.Min(MathUtils.Min(pos0, pos60), pos120);
        }

        // <summary>
        /// Returns whether an atom and the atom in the specified direction are both fire atoms and have no bond between them.
        /// </summary>
        private bool IsUnbondedFirePair(Atom atom, HexRotation direction)
        {
            return atom.Element == Element.Fire
                && atom.Bonds[direction] == BondType.None
                && Molecule.GetAdjacentAtom(atom.Position, direction)?.Element == Element.Fire;
        }

        private void MoveThroughBonder(GlyphType type, HexRotation direction, int count, Func<Atom, bool> shouldBondAtom)
        {
            for (int i = 0; i < count; i++)
            {
                Add(Instruction.MovePositive);
                var atom = Molecule.GetAtom(new Vector2(m_assemblyArea.Width - 1 - i, Row));
                if (atom != null && shouldBondAtom(atom))
                {
                    Add(Instruction.Retract, Instruction.Extend);
                    m_assemblyArea.SetUsedBonders(type, direction);
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
