namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    /// <summary>
    /// Assembles molecules from their component atoms.
    /// </summary>
    public abstract class MoleculeAssembler : SolverComponent
    {
        public MoleculeAssembler(SolverComponent parent, ProgramWriter writer, Vector2 position)
            : base(parent, writer, position)
        {
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
