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
        private ProgramWriter m_writer;
        private IEnumerable<Molecule> m_products;
        private IReadOnlyDictionary<int, int> m_productCopyCounts;

        private SimpleOutputArea m_outputArea;

        public OutputGenerator(CommandSequence commandSequence, ProgramWriter writer, IEnumerable<Molecule> products,
            Recipe recipe, IReadOnlyDictionary<int, int> productCopyCounts)
            : base(commandSequence, recipe)
        {
            m_writer = writer;
            m_products = products;
            m_productCopyCounts = productCopyCounts;

            m_outputArea = new SimpleOutputArea(m_writer, m_products);
        }

        protected override bool CanGenerateElement(Element element) => false;

        public void GenerateCommandSequence()
        {
            foreach (var product in m_products)
            {
                var elementOrder = m_outputArea.GetProductElementOrder(product);
                for (int i = 0; i < m_productCopyCounts[product.ID]; i++)
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
