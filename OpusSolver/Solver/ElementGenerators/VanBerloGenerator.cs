using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates a cardinal element from salt using Van Berlo's wheel.
    /// </summary>
    public class VanBerloGenerator : ElementGenerator
    {
        public VanBerloGenerator(CommandSequence commandSequence, SolutionPlan plan)
            : base(commandSequence, plan)
        {
        }

        // We need to always generate passthrough commands for this generator, so that the
        // atom generator will know whether it needs to rotate the wheel to salt so that the
        // passed-through salt atom doesn't get converted into something else.
        public override bool AlwaysRequiresPassthrough => true;

        protected override bool CanGenerateElement(Element element)
        {
            return Recipe.HasAvailableReactions(ReactionType.VanBerlo, outputElement: element);
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            // We need salt to convert an atom to a cardinal but we'll request everything in possibleElements too.
            // That way, if the input area actually has an atom of the requested cardinal element, it'll give us
            // that one rather than a salt.
            var generated = Parent.RequestElement(possibleElements.Concat([Element.Salt]));
            if (generated == Element.Salt)
            {
                // If more than one possible cardinal was requested, arbitrarily pick the first one
                var element = possibleElements.First();
                CommandSequence.Add(CommandType.Consume, Element.Salt, this);
                CommandSequence.Add(CommandType.Generate, element, this);
                Recipe.RecordReactionUsage(ReactionType.VanBerlo, outputElement: element);

                return element;
            }
            else
            {
                PassThrough(generated);
                return generated;
            }
        }
    }
}
