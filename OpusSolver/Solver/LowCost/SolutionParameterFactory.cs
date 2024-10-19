using System.Linq;

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
            
            if (puzzle.Products.Any(p => p.Atoms.Count() > 1))
            {
                registry.AddParameter(SolutionParameterRegistry.Common.ReverseProductElementOrder);
            }

            if (puzzle.Reagents.Any(p => p.Atoms.Count() > 1))
            {
                registry.AddParameter(SolutionParameterRegistry.Common.ReverseReagentElementOrder);
            }

            return registry;
        }
    }
}