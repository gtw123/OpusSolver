namespace OpusSolver.Solver.LowCost
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
            /*
            if (puzzle.Products.Any(p => p.Atoms.Count() > 0))
            {
                registry.AddParameter(SolutionParameterRegistry.Common.ReverseProductElementOrder);
            }*/

            return registry;
        }
    }
}