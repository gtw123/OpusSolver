namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Temporarily stores elements that aren't needed by the rest of the pipeline yet.
    /// </summary>
    public abstract class ElementBuffer : ElementGenerator
    {
        public ElementBuffer(CommandSequence commandSequence, SolutionPlan plan)
            : base(commandSequence, plan)
        {
        }

        public abstract bool CanRestoreElement(Element element);

        public abstract void StoreElement(Element element);
    }
}
