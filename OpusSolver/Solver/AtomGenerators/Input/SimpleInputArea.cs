using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;
using OpusSolver.Solver.AtomGenerators.Input.Dissassemblers;

namespace OpusSolver.Solver.AtomGenerators.Input
{
    /// <summary>
    /// A simple input area used when all reagents have a single atom.
    /// </summary>
    public class SimpleInputArea : AtomGenerator
    {
        public const int MaxReagents = 4;

        public override Vector2 OutputPosition => new Vector2();
        private List<MonoatomicDisassembler> m_disassemblers = new List<MonoatomicDisassembler>();

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

            var dir = HexRotation.R180;
            foreach (var reagent in reagents)
            {
                m_disassemblers.Add(new MonoatomicDisassembler(this, Writer, new Vector2(0, 0), reagent, dir, Instruction.Extend));
                dir = dir.Rotate60Clockwise();
            }
        }

        public override void Generate(Element element, int id)
        {
            var disassembler = m_disassemblers.Single(i => i.Molecule.ID == id);
            disassembler.GetNextAtom();
        }
    }
}
