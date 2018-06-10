using System.Drawing;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Analyzes the glyph palette in the sidebar.
    /// </summary>
    public class GlyphPaletteAnalyzer : Analyzer
    {
        /// <summary>
        /// Offset from the top left of the palette to the center of the first glyph.
        /// </summary>
        private static readonly Point GlyphOffset = new Point(62, 66);

        private const int GlyphColumnWidth = 122;
        private const int GlyphRowHeight = 115;

        private Palette<GlyphType, GlyphType> m_palette;
        private GlyphAnalyzer m_glyphAnalyzer;

        public GlyphPaletteAnalyzer(Palette<GlyphType, GlyphType> palette, ScreenCapture capture)
            : base(capture)
        {
            m_palette = palette;
            m_glyphAnalyzer = new GlyphAnalyzer(capture);
        }

        public void Analyze()
        {
            int numCols = Capture.Rect.Width / GlyphColumnWidth;
            int numRows = Capture.Rect.Height / GlyphRowHeight;

            if (numCols <= 0 || numRows <= 0)
            {
                throw new AnalysisException(Invariant($"The glyphs palette is smaller than expected. Dimensions are: {Capture.Rect.Size}."));
            }

            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    var location = new Point(col * GlyphColumnWidth, row * GlyphRowHeight).Add(GlyphOffset);
                    var type = m_glyphAnalyzer.Analyze(location);

                    // The last glyph may be centred on the last row
                    if (type == null && row == numRows - 1 && col == 0)
                    {
                        col++;
                        location.X += GlyphColumnWidth / 2;
                        type = m_glyphAnalyzer.Analyze(location);
                    }

                    if (type == null)
                    {
                        throw new AnalysisException(Invariant($"Can't identify the glyph at {location}."));
                    }

                    m_palette.AddTool(type.Value, new Tool<GlyphType>(type.Value, location));
                }
            }
        }
    }
}
