using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace Opus
{
    public class Atom
    {
        public Element Element { get; set; }

        /// <summary>
        /// Coordinates of this atom relative to the origin of the molecule.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// The bonds of this atom in each of the 6 directions around it.
        /// </summary>
        public List<BondType> Bonds { get; private set; }

        public Atom(Element element, IEnumerable<BondType> bonds, Vector2 position)
        {
            Element = element;
            Position = position;
            Bonds = bonds.ToList();

            if (Bonds.Count != Direction.Count)
            {
                throw new ArgumentException(Invariant($"Expected 'bonds' to have {Direction.Count} items but it instead had {Bonds.Count}'."));
            }
        }

        public override string ToString()
        {
            return Element.ToDebugString();
        }
    }
}
