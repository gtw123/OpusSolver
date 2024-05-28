using OpusSolver.Solver.AtomGenerators.Output;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates outputs (products) from other element generators.
    /// </summary>
    public class OutputGenerator : ElementGenerator
    {
        public override IEnumerable<Element> OutputElements => new Element[0];

        private ProgramWriter m_writer;
        private IEnumerable<Molecule> m_products;
        private int m_outputScale;

        private SimpleOutputArea m_outputArea;

        public OutputGenerator(CommandSequence commandSequence, ProgramWriter writer, IEnumerable<Molecule> products, int outputScale)
            : base(commandSequence)
        {
            m_writer = writer;
            m_products = products;
            m_outputScale = outputScale;

            m_outputArea = new SimpleOutputArea(m_writer, m_products);
        }

        public void GenerateCommandSequence()
        {
            bool anyRepeats = m_products.Any(product => product.HasRepeats);
            foreach (var product in m_products)
            {
                // If there's a mix of repeating and non-repeating molecules, build extra copies of the
                // non-repeating ones. This is to compensate for the fact that we build all copies of
                // the repeating molecules at the same time.
                int numCopies = (anyRepeats && !product.HasRepeats) ? 6 * m_outputScale : 1;

                var elementOrder = m_outputArea.GetProductElementOrder(product);
                for (int i = 0; i < numCopies; i++)
                {
                    foreach (var element in elementOrder)
                    {
                        CommandSequence.Add(CommandType.Consume, Parent.RequestElement(element), this, product.ID);
                    }
                }
            }
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            throw new InvalidOperationException("Can't call GenerateElement on an OutputGenerator.");
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            return m_outputArea;
        }
    }
}
