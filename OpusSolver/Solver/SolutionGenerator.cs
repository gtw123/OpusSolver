using OpusSolver.Utils;
using System;

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
        private SolutionParameterSet m_paramSet;

        private ISolutionBuilder m_solutionBuilder;
        private ProgramWriter m_writer = new ProgramWriter();

        public SolutionGenerator(Puzzle puzzle, SolutionType solutionType, Recipe recipe, SolutionParameterSet paramSet)
        {
            m_puzzle = puzzle;
            m_solutionType = solutionType;
            m_recipe = recipe;
            m_paramSet = paramSet;
        }

        private ISolutionBuilder CreateSolutionBuilder() => m_solutionType switch
        {
            SolutionType.Standard => new Standard.SolutionBuilder(m_puzzle, m_recipe, m_paramSet, m_writer),
            SolutionType.LowCost => new LowCost.SolutionBuilder(m_puzzle, m_recipe, m_paramSet, m_writer),
            _ => throw new ArgumentException($"Invalid solution type {m_solutionType}.")
        };

        public Solution Generate()
        {
            Arm.ResetArmIDs();

            m_solutionBuilder = CreateSolutionBuilder();

            var plan = m_solutionBuilder.CreatePlan();
            var pipeline = new ElementPipeline(plan);
            var commandSequence = pipeline.GenerateCommandSequence();

            try
            {
                GenerateProgramFragments(pipeline, commandSequence);
            }
            catch (UnsupportedException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogUtils.LogSolverException(m_puzzle.Name, m_puzzle.FilePath, e, logToConsole: false);

                // Try to generate a solution anyway so that it can be saved to disk and the user can debug it
                var solution2 = CreateSolution();
                solution2.Exception = e;
                return solution2;
            }

            var solution = CreateSolution();
            return OptimizeSolution(solution);
        }

        /// <summary>
        /// Generates the program fragments for the solution.
        /// </summary>
        private void GenerateProgramFragments(ElementPipeline pipeline, CommandSequence commandSequence)
        {
            sm_log.Debug("Generating program fragments");

            m_solutionBuilder.CreateAtomGenerators(pipeline);

            var generators = pipeline.ElementGenerators;
            foreach (var generator in generators)
            {
                generator.AtomGenerator.BeginSolution();
            }

            foreach (var command in commandSequence.Commands)
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

        private Solution CreateSolution()
        {
            sm_log.Debug("Creating solution");

            string name = $"Generated solution ({m_solutionType})";
            var objects = m_solutionBuilder.GetAllObjects();
            var program = new ProgramBuilder(m_writer.Fragments).Build();
            return new Solution(m_puzzle, name, objects, program);
        }

        private Solution OptimizeSolution(Solution solution)
        {
            sm_log.Debug("Optimizing solution");
            new CostOptimizer(solution).Optimize();

            return solution;
        }
    }
}