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

        public Solution Solve()
        {
            sm_log.Debug("Solving puzzle");

            Arm.ResetArmIDs();

            CheckPreconditions();
            FixRepeatingMolecules();

            var generator = new RecipeGenerator(Puzzle);
            var solution = new SolutionGenerator(Puzzle, m_solutionType, generator.GenerateRecipe()).Generate();
            CheckAllowedGlyphs(solution);

            return solution;
        }

        private void CheckPreconditions()
        {
            if (Puzzle.Products.Any(p => p.Atoms.Any(a => a.Bonds.Values.Any(b => b == BondType.Triplex) && a.Element != Element.Fire)))
            {
                throw new SolverException("This puzzle has triplex bonds between non-fire atoms.");
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
                throw new SolverException("This puzzle doesn't allow the glyph of bonding.");
            }

            if (missingGlyphs.Contains(GlyphType.Unbonding))
            {
                throw new SolverException("This puzzle doesn't allow the glyph of unbonding.");
            }

            if (missingGlyphs.Contains(GlyphType.TriplexBonding))
            {
                throw new SolverException("One or more products contain triplex bonds but the puzzle doesn't allow the glyph of triplex bonding.");
            }
        }
    }
}
