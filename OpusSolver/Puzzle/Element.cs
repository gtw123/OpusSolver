using System;

namespace OpusSolver
{
    public enum Element
    {
        Salt,
        Air,
        Fire,
        Water,
        Earth,
        Quicksilver,
        Lead,
        Tin,
        Iron,
        Copper,
        Silver,
        Gold,
        Mors,
        Vitae,
        Quintessence,
        Repeat
    }

    public static class ElementExtensions
    {
        public static string ToDebugString(this Element element)
        {
            return element switch
            {
                Element.Salt => "Sa",
                Element.Air => "Ai",
                Element.Fire => "Fi",
                Element.Water => "Wa",
                Element.Earth => "Ea",
                Element.Quicksilver => "Qs",
                Element.Lead => "Pb",
                Element.Tin => "Sn",
                Element.Iron => "Fe",
                Element.Copper => "Cu",
                Element.Silver => "Ag",
                Element.Gold => "Au",
                Element.Mors => "Mo",
                Element.Vitae => "Vi",
                Element.Quintessence => "Qt",
                Element.Repeat => "..",
                _ => throw new ArgumentException($"Unknown element {element}.")
            };
        }
    }
}
