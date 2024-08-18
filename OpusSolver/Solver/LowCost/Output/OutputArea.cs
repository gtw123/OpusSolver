namespace OpusSolver.Solver.LowCost.Output
{
    /// <summary>
    /// An output area which assembles products using a single assembler.
    /// </summary>
    public class OutputArea : LowCostAtomGenerator
    {
        private MoleculeAssembler m_assembler;

        public OutputArea(ProgramWriter writer, ArmArea armArea, MoleculeAssemblerFactory assemblerFactory)
            : base(writer, armArea)
        {
            m_assembler = assemblerFactory.CreateAssembler(this, Writer, armArea);
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
