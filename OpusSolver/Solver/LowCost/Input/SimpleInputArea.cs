using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// A simple input area used when all reagents have a single atom.
    /// </summary>
    public class SimpleInputArea : LowCostAtomGenerator
    {
        private List<MoleculeDisassembler> m_disassemblers = new List<MoleculeDisassembler>();

        public const int MaxReagents = 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => m_disassemblers.SelectMany(d => d.RequiredAccessPoints.Select(p => d.Transform.Apply(p)));

        public SimpleInputArea(ProgramWriter writer, ArmArea armArea, IEnumerable<Molecule> reagents)
            : base(writer, armArea)
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
                m_disassemblers.Add(new SingleMonoatomicDisassembler(this, Writer, ArmArea, new Transform2D(), reagentsList[0]));
                return;
            }

            var pos = new Vector2(ArmArea.ArmLength, 0).RotateBy(HexRotation.R240);
            var transform = new Transform2D(pos, HexRotation.R300);
            m_disassemblers.Add(new SingleMonoatomicDisassembler(this, Writer, ArmArea, transform, reagentsList[0]));

            if (reagentsList.Count > 1)
            {
                transform.Position += new Vector2(1, -1);
                m_disassemblers.Add(new SingleMonoatomicDisassembler(this, Writer, ArmArea, transform, reagentsList[1]));
            }
        }

        public override void Generate(Element element, int id)
        {
            var disassembler = m_disassemblers.Single(i => i.Molecule.ID == id);
            disassembler.GenerateNextAtom();
        }
    }
}
