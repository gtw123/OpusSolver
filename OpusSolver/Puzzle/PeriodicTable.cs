using System.Collections.Generic;
using System.Linq;

namespace OpusSolver
{
    public static class PeriodicTable
    {
        public static IEnumerable<Element> AllElements => new[] { Element.Salt, Element.Quicksilver, Element.Quintessence }.Concat(Cardinals).Concat(Metals).Concat(MorsVitae);
        public static IEnumerable<Element> Cardinals => new[] { Element.Air, Element.Fire, Element.Water, Element.Earth };
        public static IReadOnlyList<Element> Metals => new[] { Element.Lead, Element.Tin, Element.Iron, Element.Copper, Element.Silver, Element.Gold };
        public static IEnumerable<Element> MorsVitae => new[] { Element.Mors, Element.Vitae };

        public static int GetMetalDifference(Element sourceMetal, Element destMetal)
        {
            return (int)destMetal - (int)sourceMetal;
        }

        public static Element GetLowestMetal(Element metal1, Element metal2)
        {
            return (metal1 < metal2) ? metal1 : metal2;
        }

        private readonly static Dictionary<Element, int> sm_metalPurities = new()
        {
            { Element.Lead, 1 },
            { Element.Tin, 2 },
            { Element.Iron, 4 },
            { Element.Copper, 8 },
            { Element.Silver, 16 },
            { Element.Gold, 32 },
        };

        public static int GetMetalPurity(Element metal) => sm_metalPurities[metal];

        public static IEnumerable<Element> GetMetalsWithPuritySameOrLower(int purity)
            => sm_metalPurities.Where(p => p.Value <= purity).Select(p => p.Key);
    }
}
