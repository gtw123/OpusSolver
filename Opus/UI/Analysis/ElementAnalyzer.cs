using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Identifies elements on the screen.
    /// </summary>
    public class ElementAnalyzer : Analyzer
    {
        private static Dictionary<MoleculeType, Dictionary<Element, ReferenceImage>> sm_referenceImages = new Dictionary<MoleculeType, Dictionary<Element, ReferenceImage>>();

        private MoleculeType m_type;

        static ElementAnalyzer()
        {
            var productThresholds = new Dictionary<Element, ThresholdData>() {
                [ Element.Salt ] =          new ThresholdData { Lower = 0.35f, Upper = 0.45f },
                [ Element.Air ] =           new ThresholdData { Lower = 0.35f, Upper = 0.45f },
                [ Element.Fire ] =          new ThresholdData { Lower = 0.28f, Upper = 0.35f },
                [ Element.Water ] =         new ThresholdData { Lower = 0.37f, Upper = 0.45f },
                [ Element.Earth ] =         new ThresholdData { Lower = 0.30f, Upper = 0.45f },
                [ Element.Quicksilver ] =   new ThresholdData { Lower = 0.35f, Upper = 0.45f },
                [ Element.Lead ] =          new ThresholdData { Lower = 0.30f, Upper = 0.45f },
                [ Element.Tin ] =           new ThresholdData { Lower = 0.25f, Upper = 0.45f },
                [ Element.Iron ] =          new ThresholdData { Lower = 0.30f, Upper = 0.50f },
                [ Element.Copper ] =        new ThresholdData { Lower = 0.20f, Upper = 0.40f },
                [ Element.Silver ] =        new ThresholdData { Lower = 0.25f, Upper = 0.45f },
                [ Element.Gold ] =          new ThresholdData { Lower = 0.20f, Upper = 0.45f },
                [ Element.Mors ] =          new ThresholdData { Lower = 0.25f, Upper = 0.40f },
                [ Element.Vitae ] =         new ThresholdData { Lower = 0.35f, Upper = 0.50f },
                [ Element.Quintessence ] =  new ThresholdData { Lower = 0.30f, Upper = 0.45f },
                [ Element.Repeat ] =        new ThresholdData { Lower = 0.00f, Upper = 0.10f }
            };

            var reagentThresholds = new Dictionary<Element, ThresholdData>() {
                [ Element.Salt ] =          new ThresholdData { Lower = 0.90f, Upper = 1.00f },
                [ Element.Air ] =           new ThresholdData { Lower = 0.90f, Upper = 1.00f },
                [ Element.Fire ] =          new ThresholdData { Lower = 0.69f, Upper = 0.80f },
                [ Element.Water ] =         new ThresholdData { Lower = 0.85f, Upper = 1.00f },
                [ Element.Earth ] =         new ThresholdData { Lower = 0.78f, Upper = 1.00f },
                [ Element.Quicksilver ] =   new ThresholdData { Lower = 0.90f, Upper = 1.00f },
                [ Element.Lead ] =          new ThresholdData { Lower = 0.85f, Upper = 1.00f },
                [ Element.Tin ] =           new ThresholdData { Lower = 0.85f, Upper = 1.00f },
                [ Element.Iron ] =          new ThresholdData { Lower = 0.90f, Upper = 1.00f },
                [ Element.Copper ] =        new ThresholdData { Lower = 0.90f, Upper = 1.00f },
                [ Element.Silver ] =        new ThresholdData { Lower = 0.93f, Upper = 1.00f },
                [ Element.Gold ] =          new ThresholdData { Lower = 0.85f, Upper = 1.00f },
                [ Element.Mors ] =          new ThresholdData { Lower = 0.60f, Upper = 0.83f },
                [ Element.Vitae ] =         new ThresholdData { Lower = 0.85f, Upper = 1.00f },
                [ Element.Quintessence ] =  new ThresholdData { Lower = 0.90f, Upper = 1.00f }
            };

            LoadReferenceImages(MoleculeType.Product, productThresholds);
            LoadReferenceImages(MoleculeType.Reagent, reagentThresholds);
        }

        private static void LoadReferenceImages(MoleculeType type, Dictionary<Element, ThresholdData> thresholds)
        {
            sm_referenceImages[type] = new Dictionary<Element, ReferenceImage>();

            foreach (var (element, thresholdData) in thresholds)
            {
                string file = Invariant($"Opus.Images.Elements.{type}.{element}.png");
                sm_referenceImages[type][element] = ReferenceImage.CreateBrightnessThresholdedImage(file, thresholdData, 20);
            }
        }

        public ElementAnalyzer(ScreenCapture capture, MoleculeType type)
            : base(capture)
        {
            m_type = type;
        }

        public Element? Analyze(Point location)
        {
            foreach (var (element, image) in sm_referenceImages[m_type])
            {
                if (image.IsMatch(Capture.Bitmap, location))
                {
                    return element;
                }
            }

            return null;
        }

        public (int smallest, int nextSmallest) CalculateDifferences(Point location)
        {
            var diffs = sm_referenceImages[m_type].Values.Select(image => image.CalculateDifference(Capture.Bitmap, location));
            var sorted = diffs.OrderBy(x => x).ToList();
            return (sorted[0], sorted[1]);
        }
    }
}
