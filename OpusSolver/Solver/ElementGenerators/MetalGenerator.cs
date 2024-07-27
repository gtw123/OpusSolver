using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates a metal element.
    /// </summary>
    public abstract class MetalGenerator : ElementGenerator
    {
        protected MetalGenerator(CommandSequence commandSequence, Recipe recipe)
            : base(commandSequence, recipe)
        {
        }

        protected abstract ReactionType ReactionType { get; }

        protected IEnumerable<Element> GetAvailableSourceElementsForTarget(Element targetElement)
        {
            var sourceElement = targetElement - 1;

            // We need to have a complete pathway from a source element to the target element, so we
            // work backwards from the targetElement
            while (sourceElement >= PeriodicTable.Metals.First())
            {
                if (!Recipe.HasAvailableReactions(ReactionType, inputElement: sourceElement))
                {
                    yield break;
                }

                yield return sourceElement;
                sourceElement--;
            }
        }

        protected override bool CanGenerateElement(Element element)
        {
            return GetAvailableSourceElementsForTarget(element).Any();
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            if (possibleElements.Count() > 1)
            {
                throw new InvalidOperationException($"MetalGenerator only supports generating one type of element but {possibleElements.Count()} were specified");
            }

            var targetElement = possibleElements.First();
            var requestedElements = GetAvailableSourceElementsForTarget(targetElement);
            var receivedElement = Parent.RequestElement(requestedElements);
            if (receivedElement != targetElement)
            {
                GenerateMetal(receivedElement, targetElement);
            }
            else
            {
                PassThrough(receivedElement);
            }

            return targetElement;
        }

        protected abstract void GenerateMetal(Element firstMetal, Element targetMetal);
    }
}
