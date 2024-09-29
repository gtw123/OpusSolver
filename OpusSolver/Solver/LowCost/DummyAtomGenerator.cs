namespace OpusSolver.Solver.LowCost
{
    public class DummyAtomGenerator : LowCostAtomGenerator
    {
        public override bool IsEmpty => true;

        public DummyAtomGenerator(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
        }
    }
}
