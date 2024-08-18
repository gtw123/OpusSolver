using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates a metal from a lower metal and quicksilver.
    /// </summary>
    public class MetalProjectorGenerator : MetalGenerator
    {
        public MetalProjectorGenerator(CommandSequence commandSequence, Recipe recipe)
            : base(commandSequence, recipe)
        {
        }

        protected override ReactionType ReactionType => ReactionType.Projection;

        protected override void GenerateMetal(Element firstMetal, Element targetMetal)
        {
            int diff = PeriodicTable.GetMetalDifference(firstMetal, targetMetal);
            if (diff < 0)
            {
                throw new SolverException(Invariant($"Cannot use glyph of projection to convert {firstMetal} to {targetMetal}."));
            }

            CommandSequence.Add(CommandType.Consume, firstMetal, this);

            for (int i = 0; i < diff; i++)
            {
                CommandSequence.Add(CommandType.Consume, Parent.RequestElement(Element.Quicksilver), this);
                int metal = firstMetal - PeriodicTable.Metals.First() + i;
                Recipe.RecordReactionUsage(ReactionType.Projection, inputElement: PeriodicTable.Metals[metal]);
            }

            CommandSequence.Add(CommandType.Generate, targetMetal, this);
        }
    }
}
