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

        private IEnumerable<Molecule> m_products;
        private int m_outputScale;

        private Dictionary<int, AssemblyType> m_assemblyTypes = new();

        public OutputGenerator(CommandSequence commandSequence, IEnumerable<Molecule> products, int outputScale)
            : base(commandSequence)
        {
            m_products = products;
            m_outputScale = outputScale;

            foreach (var product in products)
            {
                m_assemblyTypes[product.ID] = DetermineAssemblyType(product);
            }
        }

        private AssemblyType DetermineAssemblyType(Molecule product)
        {
            if (product.Atoms.Count() == 1)
            {
                return AssemblyType.SingleAtom;
            }
            else if (product.Height == 1 && !product.HasTriplex)
            {
                return AssemblyType.Linear;
            }
            else if (product.Atoms.Count() == 4 && !product.HasTriplex)
            {
                if (product.GetAtom(new Vector2(1, 1)) != null)
                {
                    Vector2[] positions = [new Vector2(0, 1), new Vector2(1, 2), new Vector2(2, 0)];
                    if (positions.All(pos => product.GetAtom(pos) != null))
                    {
                        return AssemblyType.Star2;
                    }

                    positions = [new Vector2(0, 2), new Vector2(1, 0), new Vector2(2, 1)];
                    if (positions.All(pos => product.GetAtom(pos) != null))
                    {
                        product.Rotate180();
                        return AssemblyType.Star2;
                    }
                }
            }

            return AssemblyType.Universal;
        }

        private IEnumerable<Element> GetProductElementOrder(Molecule product)
        {
            var type = m_assemblyTypes[product.ID];
            return type switch
            {
                AssemblyType.SingleAtom or AssemblyType.Linear or AssemblyType.Universal => product.GetAtomsInInputOrder().Select(a => a.Element).ToList(),
                AssemblyType.Star2 => [
                    product.GetAtom(new Vector2(1, 1)).Element,
                    product.GetAtom(new Vector2(2, 0)).Element,
                    product.GetAtom(new Vector2(1, 2)).Element,
                    product.GetAtom(new Vector2(0, 1)).Element
                ],
                _ => throw new ArgumentException($"Unsupported assembler type {type}")
            };
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

                var elementOrder = GetProductElementOrder(product);
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
            if (m_products.All(p => p.Atoms.Count() == 1))
            {
                if (m_products.Count() == 1)
                {
                    return new TrivialOutputArea(writer, m_products);
                }
                else if (m_products.Count() <= SimpleOutputArea.MaxProducts)
                {
                    return new SimpleOutputArea(writer, m_products);
                }
            }

            return new ComplexOutputArea(writer, m_products, m_assemblyTypes);
        }
    }
}
