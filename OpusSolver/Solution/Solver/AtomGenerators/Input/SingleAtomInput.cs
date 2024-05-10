using System;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solution.Solver.AtomGenerators.Input
{
    /// <summary>
    /// Generates atoms a from a single-atom reagent input.
    /// </summary>
    public class SingleAtomInput : MoleculeInput
    {
        public override int Height => 1;
        public Element Element { get; private set; }

        private Arm m_outputArm;
        private Instruction m_instruction;

        public SingleAtomInput(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule, int direction, Instruction instruction)
            : base(parent, writer, position, molecule)
        {
            if (molecule.Atoms.Count() > 1)
            {
                throw new ArgumentException("SingleAtomInput can't handle molecules with multiple atoms.");
            }

            Element = molecule.Atoms.First().Element;
            m_instruction = instruction;

            CreateObjects(molecule, direction, instruction);
        }

        private void CreateObjects(Molecule molecule, int direction, Instruction instruction)
        {
            var pos = new Vector2(0, 0).OffsetInDirection(direction, 1);
            new Reagent(this, pos, 0, molecule.ID);
            if (instruction == Instruction.Extend)
            {
                m_outputArm = new Arm(this, pos * 2, DirectionUtil.Rotate180(direction), MechanismType.Piston);
            }
            else if (instruction == Instruction.RotateCounterclockwise)
            {
                var armPos = new Vector2(0, 0).OffsetInDirection(DirectionUtil.Rotate60Clockwise(direction), 1);
                m_outputArm = new Arm(this, armPos, DirectionUtil.Rotate60Counterclockwise(direction), MechanismType.Arm1);
            }
            else if (instruction == Instruction.RotateClockwise)
            {
                var armPos = new Vector2(0, 0).OffsetInDirection(DirectionUtil.Rotate60Counterclockwise(direction), 1);
                m_outputArm = new Arm(this, armPos, DirectionUtil.Rotate60Clockwise(direction), MechanismType.Arm1);
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
