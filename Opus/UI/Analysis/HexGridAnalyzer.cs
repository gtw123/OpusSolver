using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Analyzes the hex grid.
    /// </summary>
    public class HexGridAnalyzer : Analyzer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(HexGridAnalyzer));

        private const int VerticalEdgeMinLength = 40;
        private const float VerticalEdgeLowerThreshold = 0.1f;
        private const float VerticalEdgeUpperThreshold = 0.25f;

        private const int InstructionAreaOuterHeight = 300;

        public HexGridAnalyzer(ScreenCapture capture)
            : base(capture)
        {
        }

        public HexGrid Analyze()
        {
            var rect = new Rectangle(ScreenLayout.SidebarWidth, 0, Capture.Bitmap.Width - ScreenLayout.SidebarWidth, Capture.Bitmap.Height - InstructionAreaOuterHeight);
            var centerHex = FindApproximateCenterHex(rect);

            return new HexGrid(rect.Add(Capture.Rect.Location), centerHex.Add(Capture.Rect.Location));
        }

        /// <summary>
        /// Finds the approximate location of the center of a hexagon near the center of the grid.
        /// </summary>
        private Point FindApproximateCenterHex(Rectangle gridRect)
        {
            var gridCenter = new Point((gridRect.Left + gridRect.Right) / 2, (gridRect.Top + gridRect.Bottom) / 2);
            sm_log.Info(Invariant($"Finding center hex. Starting location: {gridCenter}"));

            var edges = FindVerticalEdges(gridCenter);

            // Determine the edge that gives a hex closest to the center of the grid on the screen
            var centers = edges.Select(edge => new Point(edge.Left + HexGrid.HexWidth / 2, edge.Top + edge.Height / 2));
            var hexCenter = centers.MinBy(center => center.DistanceToSquared(gridCenter));

            // Note that this center is only approximate since we estimated the vertical position based on the height of
            // the vertical edge. It's difficult trying to find the exact position of the top of the hex. It doesn't
            // matter for now - we'll recalibrate it later on.
            sm_log.Info(Invariant($"Approximate center of hex: {hexCenter}"));

            if (ScreenCapture.LoggingEnabled)
            {
                Capture.Bitmap.SetPixel(gridCenter.X, gridCenter.Y, Color.Yellow);
                Capture.Bitmap.SetPixel(hexCenter.X, hexCenter.Y, Color.Purple);
                Capture.Save();
            }

            return hexCenter;
        }

        private IEnumerable<Rectangle> FindVerticalEdges(Point startLocation)
        {
            // Use an area covering two hexes to maximise our chances of finding an edge
            var searchRect = new Rectangle(startLocation.X - HexGrid.HexWidth, startLocation.Y - HexGrid.HexHeight, HexGrid.HexWidth * 2, HexGrid.HexHeight * 2);
            sm_log.Info(Invariant($"Looking for vertical edge of hex in {searchRect}"));
            var edges = LineLocator.FindVerticalLines(Capture.Bitmap, searchRect, VerticalEdgeMinLength,
                col => col.IsWithinBrightnessThresholds(VerticalEdgeLowerThreshold, VerticalEdgeUpperThreshold)).ToList();

            if (ScreenCapture.LoggingEnabled)
            {
                using (Graphics g = Graphics.FromImage(Capture.Bitmap))
                {
                    g.DrawRectangle(new Pen(Color.White, 1.0f), searchRect);
                    var pen = new Pen(Color.Red, 1.0f);
                    foreach (var edge in edges)
                    {
                        g.DrawRectangle(pen, edge);
                    }
                }
            }

            if (!edges.Any())
            {
                throw new AnalysisException("Can't find any vertical edges in the hex grid.");
            }

            return edges;
        }
    }
}
