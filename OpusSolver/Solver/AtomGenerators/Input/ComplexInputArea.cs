using OpusSolver.Solver.AtomGenerators.Input;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators
{
    /// <summary>
    /// An input area that decomposes reagent molecules into single atoms.
    /// </summary>
    public class ComplexInputArea : AtomGenerator
    {
        private List<MoleculeInput> m_inputs = new List<MoleculeInput>();
        private AtomConveyor m_conveyor;

        public override Vector2 OutputPosition => m_conveyor?.OutputPosition ?? new Vector2();

        public ComplexInputArea(ProgramWriter writer, IEnumerable<Molecule> reagents)
            : base(writer)
        {
            var multiAtomReagents = reagents.Where(r => r.Atoms.Count() > 1);
            AddMultiAtomInputs(multiAtomReagents);

            var singleAtomReagents = reagents.Where(r => r.Atoms.Count() == 1);
            AddSingleAtomInputs(singleAtomReagents);

            if (m_inputs.Count > 1)
            {
                var highestInput = m_inputs.MaxBy(input => input.Position.Y);
                m_conveyor = new AtomConveyor(this, writer, new Vector2(0, 0), highestInput.Position.Y + highestInput.OutputPosition.Y);
            }
        }

        private void AddMultiAtomInputs(IEnumerable<Molecule> reagents)
        {
            foreach (var reagent in reagents)
            {
                MoleculeInput input;
                if (reagent.Height == 1)
                {
                    input = new LinearMoleculeInput(this, Writer, new Vector2(0, 0), reagent);
                }
                else
                {
                    input = new MultiAtomInput(this, Writer, new Vector2(0, 0), reagent);
                }

                if (m_inputs.Count > 0)
                {
                    // Position this input just above the previous one
                    var prevInput = m_inputs[m_inputs.Count - 1];
                    int y = prevInput.Position.Y + prevInput.Height - prevInput.HeightBelowOrigin + input.HeightBelowOrigin;

                    // Keep the Y position a multiple of 2, so that it lines up with the arms of the conveyor
                    if (y % 2 > 0)
                    {
                        y++;
                    }

                    input.Position = new Vector2(0, y);
                }

                m_inputs.Add(input);
            }
        }

        private void AddSingleAtomInputs(IEnumerable<Molecule> reagents)
        {
            int nextYPosition = 2;

            var seenElements = new HashSet<Element>();
            foreach (var reagent in reagents)
            {
                if (seenElements.Add(reagent.Atoms.First().Element))
                {
                    m_inputs.Add(new SingleAtomInput(this, Writer, new Vector2(0, nextYPosition), reagent, Direction.E, Instruction.RotateCounterclockwise));
                    nextYPosition += 2;
                }
            }
        }

        public override void Generate(Element element, int id)
        {
            var input = m_inputs.Single(i => i.Molecule.ID == id);
            input.GetNextAtom();
            m_conveyor?.MoveAtom(input.Position.Y + input.OutputPosition.Y);
        }
    }
}
