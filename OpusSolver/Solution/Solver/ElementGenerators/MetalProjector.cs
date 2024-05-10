using static System.FormattableString;

namespace OpusSolver.Solution.Solver.ElementGenerators
{
    /// <summary>
    /// Generates a metal from a lower metal and quicksilver.
    /// </summary>
    public class MetalProjectorGenerator : MetalGenerator
    {
        public MetalProjectorGenerator(CommandSequence commandSequence)
            : base(commandSequence)
        {
        }

        protected override void GenerateMetal(Element sourceMetal, Element destMetal)
        {
            int diff = PeriodicTable.GetMetalDifference(sourceMetal, destMetal);
            if (diff < 0)
            {
                throw new SolverException(Invariant($"Cannot use glyph of projection to convert {sourceMetal} to {destMetal}."));
            }

            for (int i = 0; i < diff; i++)
            {
                CommandSequence.Add(CommandType.Consume, Parent.RequestElement(Element.Quicksilver), this);
            }

            CommandSequence.Add(CommandType.Generate, destMetal, this);
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            return new AtomGenerators.MetalProjector(writer);
        }
    }
}
