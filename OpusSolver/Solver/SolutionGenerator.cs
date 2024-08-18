using System;
using System.Linq;

namespace OpusSolver.Solver
{
    /// <summary>
    /// Generates a solution to a puzzle.
    /// </summary>
    public class SolutionGenerator
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(SolutionGenerator));

        private Puzzle m_puzzle;
        private SolutionType m_solutionType;      
        private Recipe m_recipe;

        private ISolutionBuilder m_solutionBuilder;

        private CommandSequence m_commandSequence = new CommandSequence();
        private ProgramWriter m_writer = new ProgramWriter();
        private ElementPipeline m_pipeline;

        public SolutionGenerator(Puzzle puzzle, SolutionType solutionType, Recipe recipe)
        {
            m_puzzle = puzzle;
            m_solutionType = solutionType;
            m_recipe = recipe;

            m_solutionBuilder = CreateSolutionBuilder();
        }

        private ISolutionBuilder CreateSolutionBuilder() => m_solutionType switch
        {
            SolutionType.Standard => new Standard.SolutionBuilder(m_writer),
            _ => throw new ArgumentException($"Invalid solution type {m_solutionType}.")
        };

        public Solution Generate()
        {
            var plan = new SolutionPlan(m_puzzle, m_recipe, m_solutionBuilder.CreateDisassemblyStrategy, m_solutionBuilder.CreateAssemblyStrategy);

            m_pipeline = new ElementPipeline(plan, m_commandSequence);
            m_pipeline.GenerateCommandSequence();

            GenerateProgramFragments();

            var solution = CreateSolution();
            return OptimizeSolution(solution);
        }

        /// <summary>
        /// Generates the program fragments for the solution.
        /// </summary>
        private void GenerateProgramFragments()
        {
            sm_log.Debug("Generating program fragments");

            m_solutionBuilder.CreateAtomGenerators(m_pipeline);

            foreach (var command in m_commandSequence.Commands)
            {
                command.Execute();
            }

            var generators = m_pipeline.ElementGenerators;
            foreach (var generator in generators)
            {
                generator.AtomGenerator.EndSolution();
            }

            foreach (var generator in generators)
            {
                generator.AtomGenerator.OptimizeParts();
            }

            foreach (var fragment in m_writer.Fragments)
            {
                sm_log.Debug("Program fragment:" + Environment.NewLine + fragment.ToString());
            }
        }

        private Solution CreateSolution()
        {
            sm_log.Debug("Creating solution");

            var objects = m_pipeline.ElementGenerators.First().AtomGenerator.GetAllObjects();
            var program = new ProgramBuilder(m_writer.Fragments).Build();
            return new Solution(m_puzzle, objects, program);
        }

        private Solution OptimizeSolution(Solution solution)
        {
            sm_log.Debug("Optimizing solution");
            new CostOptimizer(solution).Optimize();

            return solution;
        }
    }
}