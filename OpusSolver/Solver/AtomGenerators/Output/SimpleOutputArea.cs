using System.Collections.Generic;
using System.Linq;
using OpusSolver.Solver.AtomGenerators.Output.Assemblers;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// An output area which assembles products of a specific assembly type.
    /// </summary>
    public class SimpleOutputArea : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        private MoleculeAssembler m_assembler;

        public SimpleOutputArea(ProgramWriter writer, IEnumerable<Molecule> products, AssemblyType assemblyType)
            : base(writer)
        {
            m_assembler = AssemblerFactory.CreateAssembler(assemblyType, this, writer, products);
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
