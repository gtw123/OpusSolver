using System.Linq;

namespace Opus.Solution.Solver
{
    /// <summary>
    /// Solves a puzzle!
    /// </summary>
    public class PuzzleSolver
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(PuzzleSolver));

        public Puzzle Puzzle { get; private set; }

        public PuzzleSolver(Puzzle puzzle)
        {
            Puzzle = puzzle;
        }

        public PuzzleSolution Solve()
        {
            sm_log.Debug("Solving puzzle");

            Arm.ResetArmIDs();

            CheckPreconditions();
            RotateMolecules();
            FixRepeatingMolecules();

            var solution = new SolutionGenerator(Puzzle).Generate();
            CheckAllowedGlyphs(solution);

            return solution;
        }

        private void CheckPreconditions()
        {
            if (Puzzle.Products.Any(p => p.Atoms.Any(a => a.Bonds.Any(b => b == BondType.Triplex) && a.Element != Element.Fire)))
            {
                throw new SolverException("This puzzle has triplex bonds between non-fire atoms.");
            }
        }

        private void RotateMolecules()
        {
            foreach (var molecule in Puzzle.Reagents.Concat(Puzzle.Products))
            {
                // We can't rotate repeating molecules
                if (!molecule.HasRepeats)
                {
                    // Rotate the molecule so that its shortest dimension is Y (i.e. height).
                    if (molecule.Height > molecule.Width || molecule.Height > molecule.DiagonalLength)
                    {
                        molecule.Rotate60Clockwise();

                        if (molecule.Height > molecule.Width || molecule.Height > molecule.DiagonalLength)
                        {
                            molecule.Rotate60Clockwise();
                        }
                    }
                }
            }
        }

        private void FixRepeatingMolecules()
        {
            foreach (var molecule in Puzzle.Products.Where(product => product.HasRepeats))
            {
                molecule.ExpandRepeats();
            }
        }

        private void CheckAllowedGlyphs(PuzzleSolution solution)
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
