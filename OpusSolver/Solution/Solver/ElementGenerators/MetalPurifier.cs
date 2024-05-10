using System;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates a metal from lower metals.
    /// </summary>
    public class MetalPurifierGenerator : MetalGenerator
    {
        private int m_maxSize;

        public MetalPurifierGenerator(CommandSequence commandSequence)
            : base(commandSequence)
        {
        }

        protected override void GenerateMetal(Element sourceMetal, Element destMetal)
        {
            int diff = PeriodicTable.GetMetalDifference(sourceMetal, destMetal);
            m_maxSize = Math.Max(m_maxSize, diff);

            int numAtoms = 1 << diff;
            for (int i = 1; i < numAtoms; i++)
            {
                CommandSequence.Add(CommandType.Consume, Parent.RequestElement(sourceMetal), this);
            }

            CommandSequence.Add(CommandType.Generate, destMetal, this);
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            return new AtomGenerators.MetalPurifier(writer, m_maxSize);
        }
    }
}
