namespace OpusSolver.Solver.Standard
{
    public static class SolutionParameterFactory
    {
        public static SolutionParameterRegistry CreateParameterRegistry(Puzzle puzzle, Recipe recipe)
        {
            var registry = new SolutionParameterRegistry();

            if (puzzle.Products.Count > 1)
            {
                registry.AddParameter(SolutionParameterRegistry.Common.ReverseProductBuildOrder);
            }

            return registry;
        }
    }
}