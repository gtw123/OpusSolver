using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates salt from a cardinal element.
    /// </summary>
    public class SaltGenerator : ElementGenerator
    {
        public bool RequiresPassThrough => CommandSequence.Commands.Any(c => c.Type == CommandType.PassThrough && c.ElementGenerator == this);

        public SaltGenerator(CommandSequence commandSequence, SolutionPlan plan)
            : base(commandSequence, plan)
        {
        }

        protected override bool CanGenerateElement(Element element)
        {
            return Recipe.HasAvailableReactions(ReactionType.Calcification, outputElement: element);
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            var allowedInputElements = Recipe.GetAvailableReactions(ReactionType.Calcification).SelectMany(r => r.Reaction.Inputs.Keys);
            var receivedElement = Parent.RequestElement(new[] { Element.Salt }.Concat(allowedInputElements));
            if (receivedElement != Element.Salt)
            {
                CommandSequence.Add(CommandType.Consume, receivedElement, this);
                CommandSequence.Add(CommandType.Generate, Element.Salt, this);
                Recipe.RecordReactionUsage(ReactionType.Calcification, inputElement: receivedElement);
            }
            else
            {
                PassThrough(receivedElement);
            }

            return Element.Salt;
        }
    }
}
