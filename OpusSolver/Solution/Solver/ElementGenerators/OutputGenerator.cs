using System;
using System.Collections.Generic;
using System.Linq;
using OpusSolver.Solver.AtomGenerators.Output;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates outputs (products) from other element generators.
    /// </summary>
    public class OutputGenerator : ElementGenerator
    {
        public override IEnumerable<Element> OutputElements => new Element[0];

        private IEnumerable<Molecule> m_products;

        public OutputGenerator(CommandSequence commandSequence, IEnumerable<Molecule> products)
            : base(commandSequence)
        {
            m_products = products;
        }

        public void GenerateCommandSequence()
        {
            bool anyRepeats = m_products.Any(product => product.HasRepeats);
            foreach (var product in m_products)
            {
                // If there's a mix of repeating and non-repeating molecules, build extra copies of the
                // non-repeating ones. This is to compensate for the fact that we build all copies of
                // the repeating molecules at the same time. Normally 6 copies would be enough but
                // on some journal puzzles we need 18.
                int numCopies = (anyRepeats && !product.HasRepeats) ? 18 : 1;
                for (int i = 0; i < numCopies; i++)
                {
                    foreach (var element in product.GetAtomsInInputOrder().Select(a => a.Element))
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
            if (m_products.All(p => p.Atoms.Count() == 1))
            {
                if (m_products.Count() == 1)
                {
                    return new TrivialMoleculeAssembler(writer, m_products);
                }
                else if (m_products.Count() <= SimpleMoleculeAssembler.MaxProducts)
                {
                    return new SimpleMoleculeAssembler(writer, m_products);
                }
            }

            return new ComplexMoleculeAssembler(writer, m_products);
        }
    }
}
