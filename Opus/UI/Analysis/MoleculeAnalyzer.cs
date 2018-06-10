using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Identifies the atoms in a molecule on the hex grid.
    /// </summary>
    public class MoleculeAnalyzer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(MoleculeAnalyzer));

        // The max size of the tiles to divide the grid into. We chose 4 for several reasons:
        // * It corresponds to 9 total atoms in each direction, which is the largest size molecule that can be made
        //   in the standard level editor. So most molecules are going to fit in this size of tile and won' require
        //   extra scrolling to analyze.
        // * Atoms further from the center of the grid are harder to analyze because they diverge more from the
        //   reference images (due to lighting effects). We calibrated the reference images to work with a tile
        //   size of 4.
        private const int MaxTileSize = 4;

        /// <summary>
        /// This is just to stop the analysis hanging forever if something goes wrong.
        /// </summary>
        private const int MaxIterations = 100;

        private HexGrid m_grid;
        private MoleculeType m_type;

        private HexTiling m_tiling;
        private AtomFinder m_atomFinder;

        private Dictionary<Vector2, List<Vector2>> m_tilesToAnalyze = new Dictionary<Vector2, List<Vector2>>();
        private List<Atom> m_foundAtoms = new List<Atom>();

        public MoleculeAnalyzer(HexGrid grid, MoleculeType type)
        {
            m_grid = grid;
            m_type = type;

            // Work out how many complete cells we can fit vertically
            var bounds = grid.GetVisibleCells();
            int tileSize = (bounds.Max.Y - bounds.Min.Y) / 2 - 1;
            m_tiling = new HexTiling(Math.Min(MaxTileSize, tileSize));

            m_atomFinder = new AtomFinder(grid);
        }

        public Molecule Analyze()
        {
            // For convenience, start at tile (0, 0). Even if we don't find any atoms there this will still work.
            var currentTile = new Vector2(0, 0);
            m_tilesToAnalyze[currentTile] = new List<Vector2>();

            int i = 0;
            for (; i < MaxIterations; i++)
            {
                using (var capture = new ScreenCapture(m_grid.Rect))
                {
                    FindNewCellsWithAtoms(capture);
                    AnalyzeAtomsInTile(currentTile, capture);
                    if (m_tilesToAnalyze.Count == 0)
                    {
                        break;
                    }
                }

                // Find the closest tile to the current one
                currentTile = m_tilesToAnalyze.Keys.MinBy(tile => tile.DistanceToSquared(currentTile));
                sm_log.Info(Invariant($"Current tile is now {currentTile}"));

                // Scroll the grid to the center of the tile
                m_grid.ScrollTo(m_tiling.GetCenterCell(currentTile));
            }

            if (i >= MaxIterations)
            {
                throw new AnalysisException(Invariant($"Exceeded {MaxIterations} iterations while analyzing molecule."));
            }

            if (!m_foundAtoms.Any())
            {
                throw new AnalysisException("Couldn't find any atoms for the molecule.");
            }

            return new Molecule(m_type, m_foundAtoms);
        }

        private void FindNewCellsWithAtoms(ScreenCapture capture)
        {
            sm_log.Info("Finding new cells with atoms");

            foreach (var cell in m_atomFinder.FindNewAtoms(capture))
            {
                // Get the coordinates of the tile containing this cell.
                var tile = m_tiling.GetTileForCell(cell);
                if (!m_tilesToAnalyze.TryGetValue(tile, out var tileCells))
                {
                    m_tilesToAnalyze[tile] = tileCells = new List<Vector2>();
                }

                tileCells.Add(cell);
            }
        }

        private void AnalyzeAtomsInTile(Vector2 tile, ScreenCapture capture)
        {
            sm_log.Info(Invariant($"Analyzing cells in tile {tile}"));

            var atomAnalyzer = new AtomAnalyzer(capture, m_grid, m_type);
            foreach (var cell in m_tilesToAnalyze[tile])
            {
                sm_log.Info(Invariant($"Analyzing cell at {cell}"));
                var atom = atomAnalyzer.Analyze(cell);
                if (atom == null)
                {
                    throw new AnalysisException(Invariant($"Expected to find an atom at {cell} but found none."));
                }

                m_foundAtoms.Add(atom);
            }

            m_tilesToAnalyze.Remove(tile);
        }
    }
}
