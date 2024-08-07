using OpusSolver.Solver.ElementGenerators;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    /// <summary>
    /// Represents a sequence of element generators that can be used to generate all the elements
    /// required by a puzzle.
    /// </summary>
    public class ElementPipeline
    {
        /// <summary>
        /// The sequence of element generators, ordered from inputs to outputs.
        /// </summary>
        public IEnumerable<ElementGenerator> ElementGenerators => m_generators;

        /// <summary>
        /// The output generator at the end of the pipeline.
        /// </summary>
        public OutputGenerator OutputGenerator { get; private set; }

        private Puzzle m_puzzle;
        private CommandSequence m_commandSequence;
        private ProgramWriter m_writer;

        private List<ElementGenerator> m_generators = new List<ElementGenerator>();

        public ElementPipeline(Puzzle puzzle, Recipe recipe, CommandSequence commandSequence, ProgramWriter writer)
        {
            m_puzzle = puzzle;
            m_commandSequence = commandSequence;
            m_writer = writer;

            AddGenerators(recipe);
        }

        private void AddGenerators(Recipe recipe)
        {
            AddGenerator(new InputGenerator(m_commandSequence, m_puzzle.Reagents, recipe));
            AddGenerator(new ElementBuffer(m_commandSequence, recipe));

            if (recipe.HasAvailableReactions(ReactionType.Purification))
            {
                AddGenerator(new MetalPurifierGenerator(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Projection))
            {
                AddGenerator(new MetalProjectorGenerator(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Dispersion))
            {
                AddGenerator(new QuintessenceDisperserGenerator(m_commandSequence, recipe));
                AddGenerator(new ElementBuffer(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Calcification))
            {
                AddGenerator(new SaltGenerator(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.VanBerlo))
            {
                AddGenerator(new VanBerloGenerator(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Animismus))
            {
                AddGenerator(new MorsVitaeGenerator(m_commandSequence, recipe));
                AddGenerator(new ElementBuffer(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Unification))
            {
                AddGenerator(new QuintessenceGenerator(m_commandSequence, recipe));
            }

            OutputGenerator = new OutputGenerator(m_commandSequence, m_writer, m_puzzle.Products, recipe);
            AddGenerator(OutputGenerator);
        }

        private void AddGenerator(ElementGenerator generator)
        {
            generator.Parent = m_generators.LastOrDefault();
            m_generators.Add(generator);
        }
    }
}
