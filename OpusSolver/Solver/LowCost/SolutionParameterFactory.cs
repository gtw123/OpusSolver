using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    public static class SolutionParameterFactory
    {
        public const string UseLength3Arm = nameof(UseLength3Arm);
        public const string UseBreadthFirstOrderForComplexProducts = nameof(UseBreadthFirstOrderForComplexProducts);
        public const string ReverseReagentBondTraversalDirection = nameof(ReverseReagentBondTraversalDirection);
        public const string ReverseProductBondTraversalDirection = nameof(ReverseProductBondTraversalDirection);

        public static SolutionParameterRegistry CreateParameterRegistry(Puzzle puzzle, Recipe recipe)
        {
            var registry = new SolutionParameterRegistry();

            registry.AddParameter(UseLength3Arm);

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

            bool IsSingleChain(Molecule molecule) => molecule.Atoms.All(a => a.BondCount <= 2) && molecule.Atoms.Count(a => a.BondCount == 1) == 2;
            if (puzzle.Reagents.Any(p => !IsSingleChain(p)))
            {
                registry.AddParameter(ReverseReagentBondTraversalDirection);
            }

            if (puzzle.Products.Any(p => !IsSingleChain(p)))
            {
                registry.AddParameter(UseBreadthFirstOrderForComplexProducts);
                registry.AddParameter(ReverseProductBondTraversalDirection);
            }

            return registry;
        }
    }
}