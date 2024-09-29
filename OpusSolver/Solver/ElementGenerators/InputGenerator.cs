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

        public IEnumerable<ElementInput> Inputs => m_inputs;

        public InputGenerator(CommandSequence commandSequence, SolutionPlan plan, ElementBuffer elementBuffer)
            : base(commandSequence, plan, elementBuffer)
        {
            var requiredReagents = plan.Puzzle.Reagents.Where(r => Recipe.HasAvailableReactions(ReactionType.Reagent, id: r.ID));
            m_inputs = requiredReagents.Select(reagent => new ElementInput(reagent, plan)).ToList();
        }

        protected override bool CanGenerateElement(Element element) => true;

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            var input = ChooseInput(possibleElements);
            var generated = input.GetNextElement(possibleElements);

            CommandSequence.Add(CommandType.Generate, generated, this, input.Molecule.ID);
            return generated;
        }

        private ElementInput ChooseInput(IEnumerable<Element> possibleElements)
        {
            var availableInputs = m_inputs.Where(input => Recipe.HasAvailableReactions(ReactionType.Reagent, id: input.Molecule.ID));
            var inputDistances = availableInputs.Select(input => new { input, distance = input.FindClosestElement(possibleElements) }).Where(x => x.distance != null);
            if (!inputDistances.Any())
            {
                throw new SolverException(Invariant($"Cannot find a suitable input to generate one of ({String.Join(", ", possibleElements)})."));
            }

            return inputDistances.MinBy(x => x.distance.Value).input;
        }

        protected override void AddAllPendingElements()
        {
            foreach (var input in m_inputs)
            {
                while (input.HasPendingElements)
                {
                    AddPendingElement(input.GetNextElement(PeriodicTable.AllElements), input.Molecule.ID);
                }
            }
        }
    }
}
