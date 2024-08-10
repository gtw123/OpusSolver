using OpusSolver.Solver.AtomGenerators.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates elements from inputs (reagents).
    /// </summary>
    public class InputGenerator : ElementGenerator
    {
        private List<ElementInput> m_inputs;

        public InputGenerator(CommandSequence commandSequence, IEnumerable<Molecule> reagents, Recipe recipe)
            : base(commandSequence, recipe)
        {
            m_inputs = reagents.Where(r => recipe.HasAvailableReactions(ReactionType.Reagent, id: r.ID))
                .Select(reagent => new ElementInput(reagent, recipe)).ToList();
        }

        protected override bool CanGenerateElement(Element element) => true;

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            var input = ChooseInput(possibleElements);
            var generated = input.GetNextElement();

            CommandSequence.Add(CommandType.Generate, generated, this, input.Molecule.ID);
            return generated;
        }

        private ElementInput ChooseInput(IEnumerable<Element> possibleElements)
        {
            var availableInputs = m_inputs.Where(input => Recipe.HasAvailableReactions(ReactionType.Reagent, id: input.Molecule.ID));
            var inputDistances = availableInputs.Select(input => new { input, distance = input.FindClosestElement(possibleElements) }).Where(x => x.distance != null);
            if (!inputDistances.Any())
            {
                throw new InvalidOperationException(Invariant($"Cannot find a suitable input to generate one of ({String.Join(", ", possibleElements)})."));
            }

            return inputDistances.MinBy(x => x.distance.Value).input;
        }

        public override void EndSolution()
        {
            foreach (var input in m_inputs)
            {
                while (input.HasPendingElements)
                {
                    AddPendingElement(input.GetNextElement(), input.Molecule.ID);
                }
            }
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            var usedInputs = m_inputs.Where(input => input.IsUsed);
            if (usedInputs.All(input => input.Molecule.Atoms.Count() == 1))
            {
                if (usedInputs.Count() <= SimpleInputArea.MaxReagents)
                {
                    return new SimpleInputArea(writer, usedInputs.Select(input => input.Molecule));
                }
            }

            return new ComplexInputArea(writer, usedInputs.Select(input => input.Strategy));
        }
    }
}
