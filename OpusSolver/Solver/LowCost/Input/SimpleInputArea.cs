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
        private Dictionary<int, MoleculeDisassembler> m_disassemblers = new();

        public const int MaxReagents = 4;

        // We need to manually specify the order in which to add the access points because the logic in ArmArea
        // for building the track is currently a bit simplistic.
        private List<int> m_disassemblerAccessPointOrder = new();
        public override IEnumerable<Transform2D> RequiredAccessPoints =>
            m_disassemblerAccessPointOrder.Select(o => m_disassemblers[o]).SelectMany(d => d.RequiredAccessPoints.Select(p => d.Transform.Apply(p)));

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
                AddDisassembler(reagentsList[0], new Transform2D());
                return;
            }

            var pos = new Vector2(ArmArea.ArmLength, 0).RotateBy(HexRotation.R240);

            if (reagentsList.Count > 0)
            {
                var transform = new Transform2D(pos + new Vector2(1, -1), HexRotation.R300);
                AddDisassembler(reagentsList[0], transform);
            }

            if (reagentsList.Count > 1)
            {
                var transform = new Transform2D(pos, HexRotation.R300);
                AddDisassembler(reagentsList[1], transform);
            }

            if (reagentsList.Count > 2)
            {
                var transform = new Transform2D(new Vector2(-1, 0), HexRotation.R0);
                AddDisassembler(reagentsList[2], transform);
            }

            if (reagentsList.Count > 3)
            {
                var transform = new Transform2D(new Vector2(2, -1), HexRotation.R0);
                AddDisassembler(reagentsList[3], transform, addAccessPointAtStart: true);
            }
        }

        private void AddDisassembler(Molecule reagent, Transform2D transform, bool addAccessPointAtStart = false)
        {
            var disassembler = new SingleMonoatomicDisassembler(this, Writer, ArmArea, transform, reagent);
            m_disassemblers[reagent.ID] = disassembler;

            if (addAccessPointAtStart)
            {
                m_disassemblerAccessPointOrder.Insert(0, reagent.ID);
            }
            else
            {
                m_disassemblerAccessPointOrder.Add(reagent.ID);
            }
        }

        public override void Generate(Element element, int id)
        {
            var disassembler = m_disassemblers[id];
            disassembler.GenerateNextAtom();
        }
    }
}
