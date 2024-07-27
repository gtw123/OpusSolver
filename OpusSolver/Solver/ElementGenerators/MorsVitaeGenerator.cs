using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates Mors and Vitae from salt.
    /// </summary>
    public class MorsVitaeGenerator : ElementGenerator
    {
        public MorsVitaeGenerator(CommandSequence commandSequence, Recipe recipe)
            : base(commandSequence, recipe)
        {
        }

        protected override bool CanGenerateElement(Element element)
        {
            return Recipe.HasAvailableReactions(ReactionType.Animismus, outputElement: element);
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            CommandSequence.Add(CommandType.Consume, Parent.RequestElement(Element.Salt), this);
            CommandSequence.Add(CommandType.Consume, Parent.RequestElement(Element.Salt), this);

            var firstElement = possibleElements.First();
            var secondElement = firstElement == Element.Mors ? Element.Vitae : Element.Mors;

            CommandSequence.Add(CommandType.Generate, firstElement, this);
            Recipe.RecordReactionUsage(ReactionType.Animismus);
            AddPendingElement(secondElement);

            return firstElement;
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            return new AtomGenerators.MorsVitaeGenerator(writer);
        }
    }
}
