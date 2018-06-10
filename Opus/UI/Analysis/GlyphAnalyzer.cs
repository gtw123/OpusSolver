using System;
using System.Collections.Generic;
using System.Drawing;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Identifies glyphs on the screen.
    /// </summary>
    public class GlyphAnalyzer : Analyzer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(AtomAnalyzer));

        private static Dictionary<GlyphType, ReferenceImage> sm_referenceImages = new Dictionary<GlyphType, ReferenceImage>();

        static GlyphAnalyzer()
        {
            foreach (GlyphType type in Enum.GetValues(typeof(GlyphType)))
            {
                string file = Invariant($"Opus.Images.Glyphs.{type}.png");
                sm_referenceImages[type] = ReferenceImage.CreateToleranceImage(file, 5, 0);
            }
        }

        public GlyphAnalyzer(ScreenCapture capture)
            : base(capture)
        {
        }

        public GlyphType? Analyze(Point location)
        {
            foreach (var glyphType in sm_referenceImages.Keys)
            {
                if (IsMatch(location, glyphType))
                {
                    sm_log.Info(Invariant($"Found {glyphType} glyph at {location}"));
                    return glyphType;
                }
            }

            sm_log.Info(Invariant($"Found no glyph at {location}"));
            return null;
        }

        public bool IsMatch(Point location, GlyphType glyphType)
        {
            return sm_referenceImages[glyphType].IsMatch(Capture.Bitmap, location);
        }
    }
}
