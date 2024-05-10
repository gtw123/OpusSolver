using System.Collections.Generic;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates four cardinal elements from Quintessence.
    /// </summary>
    public class QuintessenceDisperserGenerator : ElementGenerator
    {
        public QuintessenceDisperserGenerator(CommandSequence commandSequence)
            : base(commandSequence)
        {
        }

        public override IEnumerable<Element> OutputElements => PeriodicTable.Cardinals;

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            CommandSequence.Add(CommandType.Consume, Parent.RequestElement(Element.Quintessence), this);

            AddPendingElement(Element.Air);
            AddPendingElement(Element.Water);
            AddPendingElement(Element.Fire);
            CommandSequence.Add(CommandType.Generate, Element.Earth, this);
            return Element.Earth;
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            return new AtomGenerators.QuintessenceDisperser(writer);
        }
    }
}
