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
        private CommandSequence m_commandSequence = new CommandSequence();
        private ProgramWriter m_writer = new ProgramWriter();
        private ElementPipeline m_pipeline;

        public SolutionGenerator(Puzzle puzzle)
        {
            m_puzzle = puzzle;
            m_pipeline = new ElementPipeline(m_puzzle, m_commandSequence);
        }

        public PuzzleSolution Generate()
        {
            GenerateCommandSequence();
            GenerateProgramFragments();

            var solution = CreateSolution();
            return OptimizeSolution(solution);
        }

        /// <summary>
        /// Generates a sequence of commands that will be used to generate the program.
        /// We do this first rather than creating the program directly, as it gives us
        /// the opportunity to decide what components we'll need (and their positions)
        /// before creating them.
        /// </summary>
        private void GenerateCommandSequence()
        {
            sm_log.Debug("Generating command sequence");

            m_pipeline.OutputGenerator.GenerateCommandSequence();
            foreach (var generator in m_pipeline.ElementGenerators)
            {
                generator.EndSolution();
            }

            sm_log.Debug("Command sequence: " + Environment.NewLine + m_commandSequence.ToString());
        }

        /// <summary>
        /// Generates the program fragments for the solution.
        /// </summary>
        private void GenerateProgramFragments()
        {
            sm_log.Debug("Generating program fragments");

            var generators = m_pipeline.ElementGenerators;
            foreach (var generator in generators)
            {
                generator.SetupAtomGenerator(m_writer);
            }

            foreach (var command in m_commandSequence.Commands)
            {
                command.Execute();
            }

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

        private PuzzleSolution CreateSolution()
        {
            sm_log.Debug("Creating solution");

            var objects = m_pipeline.ElementGenerators.First().AtomGenerator.GetAllObjects();
            var program = new ProgramBuilder(m_writer.Fragments).Build();
            return new PuzzleSolution(m_puzzle, objects, program);
        }

        private PuzzleSolution OptimizeSolution(PuzzleSolution solution)
        {
            sm_log.Debug("Optimizing solution");
            new CostOptimizer(solution).Optimize();

            return solution;
        }
    }
}