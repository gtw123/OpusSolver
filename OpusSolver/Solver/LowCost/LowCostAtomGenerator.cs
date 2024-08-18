namespace OpusSolver.Solver.LowCost
{
    public class LowCostAtomGenerator : AtomGenerator
    {
        public ArmArea ArmArea { get; private set; }

        public LowCostAtomGenerator(ProgramWriter writer, ArmArea armArea)
            : base(writer)
        {
            ArmArea = armArea;
        }
    }
}
