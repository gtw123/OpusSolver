using System.Collections.Generic;
using System.Linq;
using OpusSolver.Solver.AtomGenerators.Output.Hex3;
using OpusSolver.Solver.AtomGenerators.Output.Universal;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// An output area which assembles products using a single assembler.
    /// </summary>
    public class SimpleOutputArea : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        private IEnumerable<Molecule> m_products;
        private MoleculeAssembler m_assembler;

        public SimpleOutputArea(ProgramWriter writer, IEnumerable<Molecule> products)
            : base(writer)
        {
            m_products = products;
            m_assembler = CreateAssembler();
        }

        private MoleculeAssembler CreateAssembler()
        {
            if (!m_products.Any(p => p.HasTriplex))
            {
                if (m_products.All(p => p.Size == 1))
                {
                    if (m_products.Count() == 1)
                    {
                        return new SingleMonoatomicAssembler(this, Writer, m_products);
                    }
                    else
                    {
                        return new MonoatomicAssembler(this, Writer, m_products);
                    }
                }
                else if (m_products.All(p => p.IsLinear)) // include monoatomic products
                {
                    return new LinearAssembler(this, Writer, m_products);
                }
                else if (m_products.All(p => Hex3Assembler.IsProductCompatible(p)))
                {
                    return new Hex3Assembler(this, Writer, m_products);
                }
            }

            return new UniversalAssembler(this, Writer, m_products);
        }

        public IEnumerable<Element> GetProductElementOrder(Molecule product)
        {
            return m_assembler.GetProductElementOrder(product);
        }

        public override void Consume(Element element, int id)
        {
            m_assembler.AddAtom(element, id);
        }

        public override void OptimizeParts()
        {
            m_assembler.OptimizeParts();
        }
    }
}
