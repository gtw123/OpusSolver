using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost.Output
{
    /// <summary>
    /// An output area which assembles products using a single assembler.
    /// </summary>
    public class OutputArea : LowCostAtomGenerator
    {
        private MoleculeAssembler m_assembler;

        public override int RequiredWidth => m_assembler.RequiredWidth;

        public override IEnumerable<Transform2D> RequiredAccessPoints => m_assembler.RequiredAccessPoints;

        public OutputArea(ProgramWriter writer, ArmArea armArea, MoleculeAssemblerFactory assemblerFactory)
            : base(writer, armArea)
        {
            m_assembler = assemblerFactory.CreateAssembler(this, Writer, armArea);
        }

        public override void BeginSolution()
        {
            m_assembler.BeginSolution();
        }

        public override void Consume(Element element, int id)
        {
            m_assembler.AddAtom(element, id);
        }

        public override void EndSolution()
        {
            ArmArea.ResetArm();
        }

        public override void OptimizeParts()
        {
            m_assembler.OptimizeParts();
        }
    }
}
