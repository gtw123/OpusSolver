using OpusSolver.Solver.AtomGenerators.Input;
using System.Collections.Generic;
using System.Linq;
using OpusSolver.Solver.AtomGenerators.Input.Dissassemblers;

namespace OpusSolver.Solver.AtomGenerators
{
    /// <summary>
    /// An input area that decomposes reagent molecules into single atoms.
    /// </summary>
    public class ComplexInputArea : AtomGenerator
    {
        private List<MoleculeDisassembler> m_disassemblers = new List<MoleculeDisassembler>();
        private AtomConveyor m_conveyor;

        public override Vector2 OutputPosition => m_conveyor?.OutputPosition ?? new Vector2();

        public ComplexInputArea(ProgramWriter writer, IEnumerable<Molecule> reagents)
            : base(writer)
        {
            var multiAtomReagents = reagents.Where(r => r.Atoms.Count() > 1);
            AddMultiAtomDisassemblers(multiAtomReagents);

            var singleAtomReagents = reagents.Where(r => r.Atoms.Count() == 1);
            AddSingleAtomDisassemblers(singleAtomReagents);

            if (m_disassemblers.Count > 1)
            {
                var highestDisassembler = m_disassemblers.MaxBy(d => d.Transform.Position.Y);
                m_conveyor = new AtomConveyor(this, writer, new Vector2(0, 0), highestDisassembler.Transform.Position.Y + highestDisassembler.OutputPosition.Y);
            }
        }

        private void AddMultiAtomDisassemblers(IEnumerable<Molecule> reagents)
        {
            foreach (var reagent in reagents)
            {
                MoleculeDisassembler dissassember;
                if (reagent.Height == 1)
                {
                    dissassember = new LinearDisassembler(this, Writer, new Vector2(0, 0), reagent);
                }
                else
                {
                    dissassember = new UniversalDisassembler(this, Writer, new Vector2(0, 0), reagent);
                }

                if (m_disassemblers.Count > 0)
                {
                    // Position this disassembler just above the previous one
                    var prevDisassembler = m_disassemblers[m_disassemblers.Count - 1];
                    int y = prevDisassembler.Transform.Position.Y + prevDisassembler.Height - prevDisassembler.HeightBelowOrigin + dissassember.HeightBelowOrigin;

                    // Keep the Y position a multiple of 2, so that it lines up with the arms of the conveyor
                    if (y % 2 > 0)
                    {
                        y++;
                    }

                    dissassember.Transform.Position = new Vector2(0, y);
                }

                m_disassemblers.Add(dissassember);
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
                    m_disassemblers.Add(new SingleAtomDisassembler(this, Writer, new Vector2(0, nextYPosition), reagent, HexRotation.R0, Instruction.RotateCounterclockwise));
                    nextYPosition += 2;
                }
            }
        }

        public override void Generate(Element element, int id)
        {
            var disassembler = m_disassemblers.Single(i => i.Molecule.ID == id);
            disassembler.GetNextAtom();
            m_conveyor?.MoveAtom(disassembler.Transform.Position.Y + disassembler.OutputPosition.Y);
        }
    }
}
