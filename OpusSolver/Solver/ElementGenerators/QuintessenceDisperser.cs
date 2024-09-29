using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates four cardinal elements from Quintessence.
    /// </summary>
    public class QuintessenceDisperserGenerator : ElementGenerator
    {
        public QuintessenceDisperserGenerator(CommandSequence commandSequence, SolutionPlan plan, ElementBuffer elementBuffer)
            : base(commandSequence, plan, elementBuffer)
        {
        }

        protected override bool CanGenerateElement(Element element)
        {
            return Recipe.HasAvailableReactions(ReactionType.Dispersion, outputElement: element);
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            CommandSequence.Add(CommandType.Consume, Parent.RequestElement(Element.Quintessence), this);

            Element generatedElement;
            if (Plan.UsePendingElementsInOrder)
            {
                // Standard solver requires a fixed element order
                generatedElement = Element.Earth;
                AddPendingElement(Element.Air);
                AddPendingElement(Element.Water);
                AddPendingElement(Element.Fire);
            }
            else
            {
                generatedElement = possibleElements.First();
                foreach (var cardinal in PeriodicTable.Cardinals.Where(e => e != generatedElement))
                {
                    AddPendingElement(cardinal);
                }
            }

            CommandSequence.Add(CommandType.Generate, generatedElement, this);
            Recipe.RecordReactionUsage(ReactionType.Dispersion);

            return generatedElement;
        }
    }
}
