using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates Quintessence from the four cardinal elements.
    /// </summary>
    public class QuintessenceGenerator : ElementGenerator
    {
        public QuintessenceGenerator(CommandSequence commandSequence)
            : base(commandSequence)
        {
        }

        public override IEnumerable<Element> OutputElements => new[] { Element.Quintessence };

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
            return Element.Quintessence;
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            return new AtomGenerators.QuintessenceGenerator(writer);
        }
    }
}
