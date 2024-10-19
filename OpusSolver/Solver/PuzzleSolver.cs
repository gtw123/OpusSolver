using OpusSolver.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    /// <summary>
    /// Solves a puzzle!
    /// </summary>
    public class PuzzleSolver
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(PuzzleSolver));

        private SolutionType m_solutionType;

        public Puzzle Puzzle { get; private set; }

        public PuzzleSolver(Puzzle puzzle, SolutionType solutionType)
        {
            Puzzle = puzzle;
            m_solutionType = solutionType;
        }

        public IEnumerable<Solution> Solve(bool generateMultipleSolutions)
        {
            sm_log.Debug("Solving puzzle");

            CheckPreconditions();
            FixRepeatingMolecules();

            var recipes = new RecipeGenerator(Puzzle, new RecipeOptions()).GenerateRecipes(generateMultipleSolutions);

            if (!generateMultipleSolutions)
            {
                return [GenerateSolution(recipes.First(), new SolutionParameterSet([]))];
            }

            recipes = recipes.Concat(new RecipeGenerator(Puzzle, new RecipeOptions { IncludeUneededReactions = true }).GenerateRecipes(generateMultipleSolutions));
            recipes = recipes.Distinct().ToList();

            var solutions = new List<Solution>();
            var exceptions = new List<Exception>();
            foreach (var recipe in recipes)
            {
                var registry = CreateParameterRegistry(recipe);
                foreach (var paramSet in registry.CreateParameterSets())
                {
                    try
                    {
                        solutions.Add(GenerateSolution(recipe.Copy(), paramSet));
                    }
                    catch (Exception e)
                    {
                        // Since we're generating multiple solutions, just log the error and continue on with the next
                        LogUtils.LogSolverException(Puzzle.Name, Puzzle.FilePath, e, logToConsole: false);
                        exceptions.Add(e);
                    }
                }
            }

            if (!solutions.Any(s => s.Exception == null))
            {
                // Unsupported messages are usually unique, but include them all if there are multiple.
                // Note that an unsupported message may be generated for one solution and not another.
                // e.g. An alternate recipe may use fewer reagents.
                var unsupportedExceptions = exceptions.OfType<UnsupportedException>().Select(e => e.Message).Distinct();
                if (unsupportedExceptions.Any())
                {
                    throw new UnsupportedException(string.Join(Environment.NewLine, unsupportedExceptions));
                }

                throw new SolverException("Could not generate any solutions. See log file for detail.");
            }

            return solutions;
        }

        private SolutionParameterRegistry CreateParameterRegistry(Recipe recipe)
        {
            return m_solutionType switch
            {
                SolutionType.Standard => Standard.SolutionParameterFactory.CreateParameterRegistry(Puzzle, recipe),
                SolutionType.LowCost => LowCost.SolutionParameterFactory.CreateParameterRegistry(Puzzle, recipe),
                _ => throw new ArgumentException($"Unknown solution type {m_solutionType}.")
            };
        }

        private Solution GenerateSolution(Recipe recipe, SolutionParameterSet paramSet)
        {
            sm_log.Debug("Recipe:" + Environment.NewLine + recipe.ToString());
            sm_log.Debug("Solution Parameters:" + Environment.NewLine + paramSet.ToString());

            var solution = new SolutionGenerator(Puzzle, m_solutionType, recipe, paramSet).Generate();
            CheckAllowedGlyphs(solution);
            return solution;
        }

        private void CheckPreconditions()
        {
            if (Puzzle.Products.Any(p => p.Atoms.Any(a => a.Bonds.Values.Any(b => b == BondType.Triplex) && a.Element != Element.Fire)))
            {
                throw new UnsupportedException("This puzzle has triplex bonds between non-fire atoms.");
            }
        }

        private void FixRepeatingMolecules()
        {
            foreach (var molecule in Puzzle.Products.Where(product => product.HasRepeats))
            {
                molecule.ExpandRepeats();
            }
        }

        private void CheckAllowedGlyphs(Solution solution)
        {
            var usedGlyphs = solution.GetObjects<Glyph>().Select(glyph => glyph.Type).Distinct();
            var missingGlyphs = usedGlyphs.Except(Puzzle.AllowedGlyphs);

            // We only check the basic glyphs as the others are checked when generating the pipeline.

            if (missingGlyphs.Contains(GlyphType.Bonding))
            {
                throw new UnsupportedException("This puzzle doesn't allow the glyph of bonding.");
            }

            if (missingGlyphs.Contains(GlyphType.Unbonding))
            {
                throw new UnsupportedException("This puzzle doesn't allow the glyph of unbonding.");
            }

            if (missingGlyphs.Contains(GlyphType.TriplexBonding))
            {
                throw new UnsupportedException("One or more products contain triplex bonds but the puzzle doesn't allow the glyph of triplex bonding.");
            }
        }
    }
}
