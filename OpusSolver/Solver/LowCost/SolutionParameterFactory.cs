﻿using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    public static class SolutionParameterFactory
    {
        public const string UseBreadthFirstOrderForComplexProducts = nameof(UseBreadthFirstOrderForComplexProducts);

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

            bool IsSingleChain(Molecule molecule) => molecule.Atoms.All(a => a.BondCount <= 2) && molecule.Atoms.Count(a => a.BondCount == 1) == 2;
            if (puzzle.Products.Any(p => !IsSingleChain(p)))
            {
                registry.AddParameter(UseBreadthFirstOrderForComplexProducts);
            }

            return registry;
        }
    }
}