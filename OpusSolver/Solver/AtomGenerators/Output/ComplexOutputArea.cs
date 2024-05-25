using System.Collections.Generic;
using System.Linq;
using OpusSolver.Solver.AtomGenerators.Output.Assemblers;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// An output area which assembles arbitrary products and moves them to their output locations.
    /// </summary>
    public class ComplexOutputArea : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        private IEnumerable<Molecule> m_products;
        private MoleculeAssembler m_assembler;

        public ComplexOutputArea(ProgramWriter writer, IEnumerable<Molecule> products)
            : base(writer)
        {
            m_products = products;

            if (m_products.All(p => p.Height == 1) && m_products.All(p => !p.HasTriplex))
            {
                m_assembler = new LinearMoleculeAssembler(this, writer, m_products);
            }
            else
            {
                m_assembler = new UniversalMoleculeAssembler(this, writer, m_products);
            }
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
