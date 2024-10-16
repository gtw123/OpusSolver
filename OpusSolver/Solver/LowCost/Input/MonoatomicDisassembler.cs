using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// A disassembler used when all reagents have a single atom.
    /// </summary>
    public class MonoatomicDisassembler : LowCostAtomGenerator
    {
        private readonly Dictionary<int, MoleculeInput> m_inputs = new();

        public const int MaxReagents = 4;

        // We need to manually specify the order in which to add the access points because the logic in ArmArea
        // for building the track is currently a bit sensitive to the order of these.
        private readonly List<int> m_inputAccessPointOrder = new();
        public override IEnumerable<Transform2D> RequiredAccessPoints =>
            m_inputAccessPointOrder.Select(o => m_inputs[o]).SelectMany(d => d.RequiredAccessPoints.Select(p => d.Transform.Apply(p)));

        public MonoatomicDisassembler(ProgramWriter writer, ArmArea armArea, IEnumerable<Molecule> reagents)
            : base(writer, armArea)
        {
            if (reagents.Any(r => r.Atoms.Count() > 1))
            {
                throw new ArgumentException($"{nameof(MonoatomicDisassembler)} can't handle reagents with multiple atoms.");
            }

            if (reagents.Count() > MaxReagents)
            {
                throw new ArgumentException(Invariant($"{nameof(MonoatomicDisassembler)} can't handle more than {MaxReagents} distinct reagents."));
            }

            CreateInputs(reagents);
        }

        private void CreateInputs(IEnumerable<Molecule> reagents)
        {
            var reagentsList = reagents.ToList();
            if (reagentsList.Count == 1)
            {
                AddInput(reagentsList[0], new Transform2D());
                return;
            }

            var pos = new Vector2(ArmArea.ArmLength, 0).RotateBy(HexRotation.R240);

            if (reagentsList.Count > 0)
            {
                var transform = new Transform2D(pos + new Vector2(1, -1), HexRotation.R300);
                AddInput(reagentsList[0], transform);
            }

            if (reagentsList.Count > 1)
            {
                var transform = new Transform2D(pos, HexRotation.R300);
                AddInput(reagentsList[1], transform);
            }

            if (reagentsList.Count > 2)
            {
                var transform = new Transform2D(new Vector2(-1, 0), HexRotation.R0);
                AddInput(reagentsList[2], transform);
            }

            if (reagentsList.Count > 3)
            {
                var transform = new Transform2D(new Vector2(2, -1), HexRotation.R0);
                AddInput(reagentsList[3], transform, addAccessPointAtStart: true);
            }
        }

        private void AddInput(Molecule reagent, Transform2D transform, bool addAccessPointAtStart = false)
        {
            var input = new MoleculeInput(this, Writer, ArmArea, transform, reagent, new Transform2D());
            m_inputs[reagent.ID] = input;

            if (addAccessPointAtStart)
            {
                m_inputAccessPointOrder.Insert(0, reagent.ID);
            }
            else
            {
                m_inputAccessPointOrder.Add(reagent.ID);
            }
        }

        public override void Generate(Element element, int id)
        {
            var input = m_inputs[id];
            input.GrabMolecule();
        }
    }
}
