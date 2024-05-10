using System;

namespace Opus
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
            string str = Enum.GetName(typeof(Element), element).Substring(0, 1);
            if (element == Element.Salt || element == Element.Quicksilver)
            {
                str = str.ToLowerInvariant();
            }

            return str;
        }
    }
}
