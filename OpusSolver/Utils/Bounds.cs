namespace OpusSolver
{
    public struct Bounds
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public Vector2 Min;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public Vector2 Max;

        public Bounds(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Bounds))
            {
                return false;
            }

            var other = (Bounds)obj;
            return other.Min.Equals(Min) && other.Max.Equals(Max);
        }

        public override int GetHashCode()
        {
            return Min.GetHashCode() ^ Max.GetHashCode();
        }

        public static bool operator == (Bounds b1, Bounds b2)
        {
            return b1.Equals(b2);
        }

        public static bool operator !=(Bounds b1, Bounds b2)
        {
            return !b1.Equals(b2);
        }
    }
}
