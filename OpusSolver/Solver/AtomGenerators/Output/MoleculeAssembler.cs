namespace OpusSolver.Solver.AtomGenerators.Output
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
        /// <returns>Whether the current product is now completely assembled and ready to move to the output location</returns>
        public abstract bool AddAtom(Element element, int productID);

        public virtual void OptimizeParts()
        {
        }
    }
}
