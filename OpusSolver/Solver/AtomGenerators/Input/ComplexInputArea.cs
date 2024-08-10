using OpusSolver.Solver.AtomGenerators.Input;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Input
{
    /// <summary>
    /// An input area that decomposes reagent molecules into single atoms.
    /// </summary>
    public class ComplexInputArea : AtomGenerator
    {
        private List<MoleculeDisassembler> m_disassemblers = new List<MoleculeDisassembler>();
        private AtomConveyor m_conveyor;

        public override Vector2 OutputPosition => m_conveyor?.OutputPosition ?? new Vector2();

        public ComplexInputArea(ProgramWriter writer, IEnumerable<DisassemblyStrategy> disassemblyStrategies)
            : base(writer)
        {
            var multiAtomReagents = disassemblyStrategies.Where(d => d.Molecule.Atoms.Count() > 1);
            AddMultiAtomDisassemblers(multiAtomReagents);

            var singleAtomReagents = disassemblyStrategies.Where(d => d.Molecule.Atoms.Count() == 1).Select(d => d.Molecule);
            if (multiAtomReagents.Count() == 1 && singleAtomReagents.Count() == 1)
            {
                // As an optimization, we don't bother creating an arm in this case
                m_disassemblers.Add(new SingleMonoatomicDisassembler(this, Writer, new Vector2(0, 2), singleAtomReagents.First()));
            }
            else
            {
                AddSingleAtomDisassemblers(singleAtomReagents);
            }

            if (m_disassemblers.Count > 1)
            {
                var highestDisassembler = m_disassemblers.MaxBy(d => d.Transform.Position.Y);
                m_conveyor = new AtomConveyor(this, writer, new Vector2(0, 0), highestDisassembler.Transform.Position.Y + highestDisassembler.OutputPosition.Y);
            }
        }

        private void AddMultiAtomDisassemblers(IEnumerable<DisassemblyStrategy> disassemblyStrategies)
        {
            foreach (var strategy in disassemblyStrategies)
            {
                var disassembler = strategy.CreateDisassembler(this, Writer, new Vector2(0, 0));
                if (m_disassemblers.Count > 0)
                {
                    // Position this disassembler just above the previous one
                    var prevDisassembler = m_disassemblers[m_disassemblers.Count - 1];
                    int y = prevDisassembler.Transform.Position.Y + prevDisassembler.Height - prevDisassembler.HeightBelowOrigin + disassembler.HeightBelowOrigin;

                    // Keep the Y position a multiple of 2, so that it lines up with the arms of the conveyor
                    if (y % 2 > 0)
                    {
                        y++;
                    }

                    disassembler.Transform.Position = new Vector2(0, y);
                }

                m_disassemblers.Add(disassembler);
            }
        }

        private void AddSingleAtomDisassemblers(IEnumerable<Molecule> reagents)
        {
            int nextYPosition = 2;

            var seenElements = new HashSet<Element>();
            foreach (var reagent in reagents)
            {
                if (seenElements.Add(reagent.Atoms.First().Element))
                {
                    m_disassemblers.Add(new MonoatomicDisassembler(this, Writer, new Vector2(0, nextYPosition), reagent, HexRotation.R0, Instruction.RotateCounterclockwise));
                    nextYPosition += 2;
                }
            }
        }

        public override void Generate(Element element, int id)
        {
            var disassembler = m_disassemblers.Single(i => i.Molecule.ID == id);
            disassembler.GenerateNextAtom();
            m_conveyor?.MoveAtom(disassembler.Transform.Position.Y + disassembler.OutputPosition.Y);
        }
    }
}
