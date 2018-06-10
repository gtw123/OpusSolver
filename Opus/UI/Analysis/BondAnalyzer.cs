using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Identifies the bonds of an atom.
    /// </summary>
    public class BondAnalyzer : Analyzer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(BondAnalyzer));

        private static Dictionary<MoleculeType, Dictionary<BondType, List<ReferenceImage>>> sm_referenceImages = new Dictionary<MoleculeType, Dictionary<BondType, List<ReferenceImage>>>();

        private MoleculeType m_type;

        private static readonly Point[] Offsets = {
            new Point(41, 0),
            new Point(20, -35),
            new Point(-21, -35),
            new Point(-41, 0),
            new Point(-21, 36),
            new Point(20, 36)
        };

        static BondAnalyzer()
        {
            var productThresholds = new Dictionary<BondType, List<ThresholdData>>
            {
                [BondType.Single] = new List<ThresholdData>
                {
                    new ThresholdData { Lower = 0.10f, Upper = 0.30f },
                    new ThresholdData { Lower = 0.10f, Upper = 0.30f },
                    new ThresholdData { Lower = 0.10f, Upper = 0.30f }
                },
                [BondType.Triplex] = new List<ThresholdData>
                {
                    // There are two variants of the triplex bond with slightly different images
                    new ThresholdData { Lower = 0.05f, Upper = 0.18f },
                    new ThresholdData { Lower = 0.05f, Upper = 0.18f },
                    new ThresholdData { Lower = 0.05f, Upper = 0.18f },
                    new ThresholdData { Lower = 0.05f, Upper = 0.18f },
                    new ThresholdData { Lower = 0.05f, Upper = 0.18f },
                    new ThresholdData { Lower = 0.05f, Upper = 0.18f }
                }
            };

            var reagentThresholds = new Dictionary<BondType, List<ThresholdData>> {
                [ BondType.Single ] = new List<ThresholdData>
                {
                    new ThresholdData { Lower = 0.00f, Upper = 0.21f },
                    new ThresholdData { Lower = 0.00f, Upper = 0.21f },
                    new ThresholdData { Lower = 0.00f, Upper = 0.21f }
                },
                [ BondType.Triplex] = new List<ThresholdData>
                {
                    new ThresholdData { Lower = 0.30f, Upper = 0.40f },
                    new ThresholdData { Lower = 0.30f, Upper = 0.40f },
                    new ThresholdData { Lower = 0.30f, Upper = 0.40f }
                }
            };

            LoadReferenceImages(MoleculeType.Product, productThresholds);
            LoadReferenceImages(MoleculeType.Reagent, reagentThresholds);
        }

        private static void LoadReferenceImages(MoleculeType type, Dictionary<BondType, List<ThresholdData>> thresholds)
        {
            sm_referenceImages[type] = new Dictionary<BondType, List<ReferenceImage>>();

            foreach (var (bondType, thresholdData) in thresholds)
            {
                sm_referenceImages[type][bondType] = new List<ReferenceImage>();

                for (int i = 0; i < thresholdData.Count; i++)
                {
                    string file = Invariant($"Opus.Images.Bonds.{type}.{bondType}{i}.png");
                    sm_referenceImages[type][bondType].Add(ReferenceImage.CreateBrightnessThresholdedImage(file, thresholdData[i], 14));
                }
            }
        }

        public BondAnalyzer(ScreenCapture capture, MoleculeType type)
            : base(capture)
        {
            m_type = type;
        }

        /// <summary>
        /// Analyzes the bonds at the specified location (assumed to be the center of an atom).
        /// </summary>
        public IEnumerable<BondType> Analyze(Point location)
        {
            for (int i = 0; i < Direction.Count; i++)
            {
                yield return AnalyzeBond(location, i);
            }
        }

        private BondType AnalyzeBond(Point location, int direction)
        {
            var compareLocation = location.Add(Offsets[direction]);

            foreach (var (bondType, refImages) in sm_referenceImages[m_type])
            {
                bool foundMatch = false;
                int imageIndex = direction % (Direction.Count / 2);

                if (refImages[imageIndex].IsMatch(Capture.Bitmap, compareLocation))
                {
                    foundMatch = true;
                }
                else
                {
                    // Try the other version of the reference image, if there is one
                    imageIndex += 3;
                    if (imageIndex < refImages.Count)
                    {
                        foundMatch = refImages[imageIndex].IsMatch(Capture.Bitmap, compareLocation);
                    }
                }

                if (foundMatch)
                {
                    sm_log.Info(Invariant($"Found {m_type} {bondType} bond in direction {direction} at {location}"));
                    return bondType;
                }
            }

            sm_log.Info(Invariant($"Found no {m_type} bond in direction {direction} at {location}"));
            return BondType.None;
        }

        public (int smallest, int nextSmallest) CalculateDifferences(Point location, int direction)
        {
            var compareLocation = location.Add(Offsets[direction]);

            var diffs = new List<int>();
            foreach (var refImages in sm_referenceImages[m_type].Values)
            {
                int imageIndex = direction % (Direction.Count / 2);
                diffs.Add(refImages[imageIndex].CalculateDifference(Capture.Bitmap, compareLocation));

                imageIndex += 3;
                if (imageIndex < refImages.Count)
                {
                    diffs.Add(refImages[imageIndex].CalculateDifference(Capture.Bitmap, compareLocation));
                }
            }

            var sorted = diffs.OrderBy(x => x).ToList();
            return (sorted[0], sorted[1]);
        }
    }
}
