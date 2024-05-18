using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// An output area which assembles arbitrary products and moves them to their output locations.
    /// </summary>
    public class ComplexOutputArea : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        private IEnumerable<Molecule> m_products;
        private UniversalMoleculeAssembler m_assembler;
        private ProductConveyor m_productConveyor;

        public ComplexOutputArea(ProgramWriter writer, IEnumerable<Molecule> products)
            : base(writer)
        {
            m_products = products;
            m_assembler = new UniversalMoleculeAssembler(this, writer, products);
            m_productConveyor = new ProductConveyor(m_assembler, writer, products);
        }

        public override void Consume(Element element, int id)
        {
            bool isProductComplete = m_assembler.AddAtom(element, id);
            if (isProductComplete)
            {
                m_productConveyor.MoveProductToOutputLocation(m_products.Single(product => product.ID == id));
            }
        }

        public override void OptimizeParts()
        {
            m_assembler.OptimizeParts();
        }
    }
}
