using System.Collections.Generic;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates four cardinal elements from Quintessence.
    /// </summary>
    public class QuintessenceDisperserGenerator : ElementGenerator
    {
        public QuintessenceDisperserGenerator(CommandSequence commandSequence, Recipe recipe)
            : base(commandSequence, recipe)
        {
        }

        protected override bool CanGenerateElement(Element element)
        {
            return Recipe.HasAvailableReactions(ReactionType.Dispersion, outputElement: element);
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            CommandSequence.Add(CommandType.Consume, Parent.RequestElement(Element.Quintessence), this);

            AddPendingElement(Element.Air);
            AddPendingElement(Element.Water);
            AddPendingElement(Element.Fire);
            CommandSequence.Add(CommandType.Generate, Element.Earth, this);
            Recipe.RecordReactionUsage(ReactionType.Dispersion);

            return Element.Earth;
        }
    }
}
