using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.Standard.Input
{
    /// <summary>
    /// A simple input area used when all reagents have a single atom.
    /// </summary>
    public class SimpleInputArea : AtomGenerator
    {
        public const int MaxReagents = 4;

        private List<MoleculeDisassembler> m_disassemblers = new List<MoleculeDisassembler>();

        public SimpleInputArea(ProgramWriter writer, IEnumerable<Molecule> reagents)
            : base(writer)
        {
            if (reagents.Any(r => r.Atoms.Count() > 1))
            {
                throw new ArgumentException($"{nameof(SimpleInputArea)} can't handle reagents with multiple atoms.");
            }

            if (reagents.Count() > MaxReagents)
            {
                throw new ArgumentException(Invariant($"{nameof(SimpleInputArea)} can't handle more than {MaxReagents} distinct reagents."));
            }

            CreateDisassemblers(reagents);
        }

        private void CreateDisassemblers(IEnumerable<Molecule> reagents)
        {
            var reagentsList = reagents.ToList();
            if (reagentsList.Count == 1)
            {
                m_disassemblers.Add(new SingleMonoatomicDisassembler(this, Writer, new Vector2(0, 0), reagentsList[0]));
                return;
            }

            var arm = new Arm(this, new Vector2(1, -1), HexRotation.R120, ArmType.Arm1, extension: 1);
            m_disassemblers.Add(new MonoatomicDisassembler(this, Writer, new Vector2(-1, 0), reagentsList[0], arm, Instruction.RotateClockwise));

            // Position other disassemblers to try to minimise cost and area
            switch (reagentsList.Count)
            {
                case 4:
                    arm = new Arm(this, new Vector2(0, 1), HexRotation.R240, ArmType.Arm1, extension: 1);
                    new Track(this, new Vector2(1, 1), HexRotation.R180, 1);
                    m_disassemblers.Add(new MonoatomicDisassembler(this, Writer, new Vector2(1, 0), reagentsList[3], arm, Instruction.MovePositive));
                    goto case 3;
                case 3:
                    arm = new Arm(this, new Vector2(0, -2), HexRotation.R60, ArmType.Arm1, extension: 2);
                    m_disassemblers.Add(new MonoatomicDisassembler(this, Writer, new Vector2(-2, 2), reagentsList[1], arm, Instruction.RotateClockwise));
                    arm = new Arm(this, new Vector2(2, -2), HexRotation.R120, ArmType.Arm1, extension: 2);
                    m_disassemblers.Add(new MonoatomicDisassembler(this, Writer, new Vector2(0, 2), reagentsList[2], arm, Instruction.RotateCounterclockwise));
                    break;
                case 2:
                    arm = new Arm(this, new Vector2(1, 0), HexRotation.R180, ArmType.Arm1, extension: 1);
                    m_disassemblers.Add(new MonoatomicDisassembler(this, Writer, new Vector2(-1, 1), reagentsList[1], arm, Instruction.RotateCounterclockwise));
                    break;
                default:
                    throw new InvalidOperationException($"Invalid reagent count: {reagentsList.Count}");
            }
        }

        public override void Generate(Element element, int id)
        {
            var disassembler = m_disassemblers.Single(i => i.Molecule.ID == id);
            disassembler.GenerateNextAtom();
        }
    }
}
