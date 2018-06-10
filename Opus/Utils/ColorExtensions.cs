using System;
using System.Drawing;

namespace Opus
{
    public static class ColorExtensions
    {
        public static bool IsSimilarTo(this Color col1, Color col2, int tolerance)
        {
            return Math.Abs(col1.R - col2.R) <= tolerance &&
                Math.Abs(col1.G - col2.G) <= tolerance &&
                Math.Abs(col1.B - col2.B) <= tolerance;
        }

        public static bool IsWithinBrightnessThresholds(this Color col, float lowerThreshold, float upperThreshold)
        {
            float brightness = col.GetBrightness();
            return brightness >= lowerThreshold && brightness <= upperThreshold;
        }

        public static Color ApplyBrightnessThreshold(this Color col, float lowerThreshold, float upperThreshold)
        {
            return col.IsWithinBrightnessThresholds(lowerThreshold, upperThreshold) ? Color.White : Color.Black;
        }

        public static bool IsWithinHueThresholds(this Color col, float lowerThreshold, float upperThreshold)
        {
            float hue = col.GetHue();
            if (lowerThreshold <= upperThreshold)
            {
                return hue >= lowerThreshold && hue <= upperThreshold;
            }
            else
            {
                // Hue is an angle, so it's fine for the lower threshold to be greater than the upper threshold
                // (e.g. >= 200 and <= 50), but we need to do the comparsion differently.
                return hue >= lowerThreshold || hue <= upperThreshold;
            }
        }

        public static Color ApplyHueThreshold(this Color col, float lowerThreshold, float upperThreshold)
        {
            return col.IsWithinHueThresholds(lowerThreshold, upperThreshold) ? Color.White : Color.Black;
        }
    }
}
