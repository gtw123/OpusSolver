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
        public MoleculeAssemblyStrategy MoleculeAssemblyStrategy { get; private set; }

        private Dictionary<int, MoleculeDisassemblyStrategy> m_disassemblyStrategies;

        public MoleculeDisassemblyStrategy GetMoleculeDisassemblyStrategy(Molecule molecule)
        {
            if (!m_disassemblyStrategies.TryGetValue(molecule.ID, out var strategy))
            {
                throw new InvalidOperationException($"No molecule disassembly strategy is defined for reagent {molecule.ID}.");
            }

            return strategy;
        }


        public SolutionPlan(Puzzle puzzle, Recipe recipe, Func<Molecule, MoleculeDisassemblyStrategy> createDisassemblyStrategy, Func<IEnumerable<Molecule>, MoleculeAssemblyStrategy> createAssemblyStrategy)
        {
            Puzzle = puzzle;
            Recipe = recipe;

            m_disassemblyStrategies = RequiredReagents.ToDictionary(r => r.ID, r => createDisassemblyStrategy(r));
            MoleculeAssemblyStrategy = createAssemblyStrategy(puzzle.Products);
        }
    }
}