using System;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver
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
        public HexRotationDictionary<BondType> Bonds { get; private set; }

        public int BondCount => Bonds.Values.Count(b => b != BondType.None);

        public Atom(Element element, HexRotationDictionary<BondType> bonds, Vector2 position)
        {
            Element = element;
            Position = position;
            Bonds = new(bonds);

            if (Bonds.Count != HexRotation.Count)
            {
                throw new ArgumentException(Invariant($"Expected 'bonds' to have {HexRotation.Count} items but it instead had {Bonds.Count}'."));
            }
        }

        public override string ToString()
        {
            return Element.ToDebugString();
        }

        public Atom Copy()
        {
            return new Atom(Element, Bonds, Position);
        }
    }
}
