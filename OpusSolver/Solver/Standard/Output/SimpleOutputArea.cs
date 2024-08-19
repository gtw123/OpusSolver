﻿namespace OpusSolver.Solver.Standard.Output
{
    /// <summary>
    /// An output area which assembles products using a single assembler.
    /// </summary>
    public class SimpleOutputArea : AtomGenerator
    {

        private MoleculeAssembler m_assembler;

        public SimpleOutputArea(ProgramWriter writer, MoleculeAssemblyStrategy assemblyStrategy)
            : base(writer)
        {
            m_assembler = assemblyStrategy.CreateAssembler(this, Writer);
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