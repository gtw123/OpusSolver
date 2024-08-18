using OpusSolver.Solver.AtomGenerators.Input;
using OpusSolver.Solver.AtomGenerators.Output;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class SolutionPlan
    {
        public Puzzle Puzzle { get; private set; }
        public Recipe Recipe { get; private set; }

        public IEnumerable<Molecule> RequiredReagents => Puzzle.Reagents.Where(r => Recipe.HasAvailableReactions(ReactionType.Reagent, id: r.ID));
        public AssemblyStrategy AssemblyStrategy { get; private set; }

        private Dictionary<int, DisassemblyStrategy> m_disassemblyStrategies;

        public DisassemblyStrategy GetDisassemblyStrategy(Molecule molecule)
        {
            if (!m_disassemblyStrategies.TryGetValue(molecule.ID, out var strategy))
            {
                throw new InvalidOperationException($"No disassembly strategy is defined for reagent {molecule.ID}.");
            }

            return strategy;
        }


        public SolutionPlan(Puzzle puzzle, Recipe recipe, Func<Molecule, DisassemblyStrategy> createDisassemblyStrategy, Func<IEnumerable<Molecule>, AssemblyStrategy> createAssemblyStrategy)
        {
            Puzzle = puzzle;
            Recipe = recipe;

            m_disassemblyStrategies = RequiredReagents.ToDictionary(r => r.ID, r => createDisassemblyStrategy(r));
            AssemblyStrategy = createAssemblyStrategy(puzzle.Products);
        }
    }
}