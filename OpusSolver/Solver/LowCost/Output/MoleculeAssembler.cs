namespace OpusSolver.Solver.LowCost.Output
{
    /// <summary>
    /// Assembles molecules from their component atoms.
    /// </summary>
    public abstract class MoleculeAssembler : SolverComponent
    {
        public ArmArea ArmArea { get; private set; }

        public MoleculeAssembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea)
            : base(parent, writer, parent.OutputPosition)
        {
            ArmArea = armArea;
        }

        /// <summary>
        /// Adds an atom to the assembly area.
        /// </summary>
        public abstract void AddAtom(Element element, int productID);

        public virtual void OptimizeParts()
        {
        }
    }
}
