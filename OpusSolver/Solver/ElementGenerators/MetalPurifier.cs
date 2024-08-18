using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates a metal from lower metals.
    /// </summary>
    public class MetalPurifierGenerator : MetalGenerator
    {
        public class PurificationSequence
        {
            public int ID;
            public Element TargetMetal;
            public Element LowestMetalUsed;
        };

        private List<PurificationSequence> m_sequences = new();

        public IReadOnlyList<PurificationSequence> Sequences => m_sequences;

        public MetalPurifierGenerator(CommandSequence commandSequence, Recipe recipe)
            : base(commandSequence, recipe)
        {
        }

        protected override ReactionType ReactionType => ReactionType.Purification;

        protected override void GenerateMetal(Element sourceMetal, Element targetMetal)
        {
            var sequence = new PurificationSequence
            {
                ID = m_sequences.Count,
                TargetMetal = targetMetal,
                LowestMetalUsed = sourceMetal
            };
            m_sequences.Add(sequence);

            CommandSequence.Add(CommandType.Consume, sourceMetal, this, sequence.ID);

            int currentMetalValue = PeriodicTable.GetMetalPurity(sourceMetal);
            int targetMetalValue = PeriodicTable.GetMetalPurity(targetMetal);

            while (currentMetalValue < targetMetalValue)
            {
                var allowableMetals = PeriodicTable.GetMetalsWithPuritySameOrLower(targetMetalValue - currentMetalValue);
                var requestedElements = allowableMetals.Intersect(GetAvailableSourceElementsForTarget(targetMetal)).ToArray();
                var receivedElement = Parent.RequestElement(requestedElements);
                CommandSequence.Add(CommandType.Consume, receivedElement, this, sequence.ID);

                sequence.LowestMetalUsed = PeriodicTable.GetLowestMetal(sequence.LowestMetalUsed, receivedElement);

                // Only record the reaction as used if the element gets combined with one we already have
                int newMetalValue = currentMetalValue + PeriodicTable.GetMetalPurity(receivedElement);
                for (var element = receivedElement; element < targetMetal; element++)
                {
                    int metalValue = PeriodicTable.GetMetalPurity(element);
                    if ((currentMetalValue & metalValue) != 0 && (newMetalValue & metalValue) == 0)
                    {
                        Recipe.RecordReactionUsage(ReactionType.Purification, inputElement: element);
                    }
                }

                currentMetalValue = newMetalValue;
            }

            CommandSequence.Add(CommandType.Generate, targetMetal, this, sequence.ID);
        }
    }
}
