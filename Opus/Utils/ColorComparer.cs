using System.Drawing;

namespace Opus
{
    public interface IColorComparer
    {
        bool Compare(Color color1, Color color2);
    }

    public class ToleranceColorComparer : IColorComparer
    {
        private int m_tolerance;

        public ToleranceColorComparer(int tolerance)
        {
            m_tolerance = tolerance;
        }

        public bool Compare(Color color1, Color color2)
        {
            return color1.IsSimilarTo(color2, m_tolerance);
        }
    }

    public class BrightnessThresholdComparer : IColorComparer
    {
        private float m_lowerThreshold;
        private float m_upperThreshold;

        public BrightnessThresholdComparer(float lowerThreshold, float upperThreshold)
        {
            m_lowerThreshold = lowerThreshold;
            m_upperThreshold = upperThreshold;
        }

        public bool Compare(Color color1, Color color2)
        {
            return color1.ApplyBrightnessThreshold(m_lowerThreshold, m_upperThreshold).ToArgb() == color2.ToArgb();
        }
    }

    public class HueThresholdComparer : IColorComparer
    {
        private float m_lowerThreshold;
        private float m_upperThreshold;

        public HueThresholdComparer(float lowerThreshold, float upperThreshold)
        {
            m_lowerThreshold = lowerThreshold;
            m_upperThreshold = upperThreshold;
        }

        public bool Compare(Color color1, Color color2)
        {
            return color1.ApplyHueThreshold(m_lowerThreshold, m_upperThreshold).ToArgb() == color2.ToArgb();
        }
    }
}
