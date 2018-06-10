using System.Collections.Generic;
using System.Linq;

namespace Opus.Solution.Solver.ElementGenerators
{
    /// <summary>
    /// Generates a metal element.
    /// </summary>
    public abstract class MetalGenerator : ElementGenerator
    {
        protected MetalGenerator(CommandSequence commandSequence)
            : base(commandSequence)
        {
        }

        public override IEnumerable<Element> OutputElements => PeriodicTable.Metals;

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            var element = possibleElements.First();
            var generated = Parent.RequestElement(PeriodicTable.GetMetalOrLower(element));
            if (generated != element)
            {
                CommandSequence.Add(CommandType.PrepareToGenerate, element, this);
                CommandSequence.Add(CommandType.Consume, generated, this);
                GenerateMetal(generated, element);
            }
            else
            {
                PassThrough(generated);
            }

            return element;
        }

        protected abstract void GenerateMetal(Element sourceMetal, Element destMetal);
    }
}
