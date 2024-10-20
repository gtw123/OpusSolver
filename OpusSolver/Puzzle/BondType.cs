using System;

namespace OpusSolver
{
    [Flags]
    public enum BondType
    {
        None = 0,
        Single = 1,
        TriplexRed = 2,
        TriplexYellow = 4,
        TriplexGray = 8,
        Triplex = TriplexRed | TriplexYellow | TriplexGray,
    }

    public static class BondTypeExtensions
    {
        public static bool HasTriplexComponents(this BondType bondType) => (bondType & BondType.Triplex) != 0;
    }
}
