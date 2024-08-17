using OpusSolver.Solver.ElementGenerators;
using System;
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
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ElementPipeline));

        /// <summary>
        /// The sequence of element generators, ordered from inputs to outputs.
        /// </summary>
        public IEnumerable<ElementGenerator> ElementGenerators => m_generators;

        /// <summary>
        /// The output generator at the end of the pipeline.
        /// </summary>
        private OutputGenerator m_outputGenerator;

        private Puzzle m_puzzle;
        private CommandSequence m_commandSequence;

        private List<ElementGenerator> m_generators = new List<ElementGenerator>();

        public ElementPipeline(Puzzle puzzle, Recipe recipe, CommandSequence commandSequence)
        {
            m_puzzle = puzzle;
            m_commandSequence = commandSequence;

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

            m_outputGenerator = new OutputGenerator(m_commandSequence, m_puzzle.Products, recipe);
            AddGenerator(m_outputGenerator);
        }

        private void AddGenerator(ElementGenerator generator)
        {
            generator.Parent = m_generators.LastOrDefault();
            m_generators.Add(generator);
        }

        /// <summary>
        /// Generates a sequence of commands that will be used to generate the program.
        /// We do this first rather than creating the program directly, as it gives us
        /// the opportunity to decide what components we'll need (and their positions)
        /// before creating them.
        /// </summary>
        public void GenerateCommandSequence()
        {
            sm_log.Debug("Generating command sequence");

            m_outputGenerator.GenerateCommandSequence();
            foreach (var generator in ElementGenerators)
            {
                generator.EndSolution();
            }

            sm_log.Debug("Command sequence: " + Environment.NewLine + m_commandSequence.ToString());
        }
    }
}
