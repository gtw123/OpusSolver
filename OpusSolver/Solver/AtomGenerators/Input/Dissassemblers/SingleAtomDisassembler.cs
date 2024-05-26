using System;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.AtomGenerators.Input.Dissassemblers
{
    /// <summary>
    /// Generates atoms a from a single-atom reagent molecule.
    /// </summary>
    public class SingleAtomDisassembler : MoleculeDisassembler
    {
        public override int Height => 1;
        public Element Element { get; private set; }

        private Arm m_outputArm;
        private Instruction m_instruction;

        public SingleAtomDisassembler(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule, HexRotation direction, Instruction instruction)
            : base(parent, writer, position, molecule)
        {
            if (molecule.Atoms.Count() > 1)
            {
                throw new ArgumentException("SingleAtomDisassembler can't handle molecules with multiple atoms.");
            }

            Element = molecule.Atoms.First().Element;
            m_instruction = instruction;

            CreateObjects(molecule, direction, instruction);
        }

        private void CreateObjects(Molecule molecule, HexRotation direction, Instruction instruction)
        {
            var pos = new Vector2(0, 0).OffsetInDirection(direction, 1);
            new Reagent(this, pos, HexRotation.R0, molecule.ID);
            if (instruction == Instruction.Extend)
            {
                m_outputArm = new Arm(this, pos * 2, direction.Rotate180(), ArmType.Piston);
            }
            else if (instruction == Instruction.RotateCounterclockwise)
            {
                var armPos = new Vector2(0, 0).OffsetInDirection(direction.Rotate60Clockwise(), 1);
                m_outputArm = new Arm(this, armPos, direction.Rotate60Counterclockwise(), ArmType.Arm1);
            }
            else if (instruction == Instruction.RotateClockwise)
            {
                var armPos = new Vector2(0, 0).OffsetInDirection(direction.Rotate60Counterclockwise(), 1);
                m_outputArm = new Arm(this, armPos, direction.Rotate60Clockwise(), ArmType.Arm1);
            }
            else
            {
                throw new ArgumentException(Invariant($"Invalid instruction '{instruction}'."));
            }
        }

        public override Element GetNextAtom()
        {
            Writer.NewFragment();
            Writer.WriteGrabResetAction(m_outputArm, m_instruction);

            return Element;
        }
    }
}
