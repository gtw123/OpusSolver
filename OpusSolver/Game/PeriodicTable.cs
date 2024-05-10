using System.Collections.Generic;
using System.Linq;

namespace Opus
{
    public static class PeriodicTable
    {
        public static IEnumerable<Element> AllElements => new[] { Element.Salt, Element.Quicksilver, Element.Quintessence }.Concat(Cardinals).Concat(Metals).Concat(MorsVitae);
        public static IEnumerable<Element> Cardinals => new[] { Element.Air, Element.Fire, Element.Water, Element.Earth };
        public static IEnumerable<Element> Metals => new[] { Element.Lead, Element.Tin, Element.Iron, Element.Copper, Element.Silver, Element.Gold };
        public static IEnumerable<Element> MorsVitae => new[] { Element.Mors, Element.Vitae };

        public static int GetMetalDifference(Element sourceMetal, Element destMetal)
        {
            return (int)destMetal - (int)sourceMetal;
        }

        public static IEnumerable<Element> GetMetalOrLower(Element metal)
        {
            for (Element element = metal; element >= Metals.First(); element--)
            {
                yield return element;
            }
        }
    }
}
