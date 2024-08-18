using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates Quintessence from the four cardinal elements.
    /// </summary>
    public class QuintessenceGenerator : ElementGenerator
    {
        public QuintessenceGenerator(CommandSequence commandSequence, Recipe recipe)
            : base(commandSequence, recipe)
        {
        }

        protected override bool CanGenerateElement(Element element)
        {
            return Recipe.HasAvailableReactions(ReactionType.Unification, outputElement: element);
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            var inputs = new HashSet<Element>(PeriodicTable.Cardinals);

            while (inputs.Any())
            {
                var element = Parent.RequestElement(inputs);
                CommandSequence.Add(CommandType.Consume, element, this);
                inputs.Remove(element);
            }

            CommandSequence.Add(CommandType.Generate, Element.Quintessence, this);
            Recipe.RecordReactionUsage(ReactionType.Unification);

            return Element.Quintessence;
        }
    }
}
