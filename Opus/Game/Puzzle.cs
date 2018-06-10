using System.Collections.Generic;
using System.Linq;

namespace Opus
{
    public class Puzzle
    {
        public List<Molecule> Products { get; private set; }
        public List<Molecule> Reagents { get; private set; }
        public HashSet<MechanismType> AllowedMechanisms { get; private set; }
        public HashSet<GlyphType> AllowedGlyphs { get; private set; }

        public Puzzle(IEnumerable<Molecule> products, IEnumerable<Molecule> reagents, IEnumerable<MechanismType> allowedMechanisms, IEnumerable<GlyphType> allowedGlyphs)
        {
            Products = products.ToList();
            Reagents = reagents.ToList();
            AllowedMechanisms = new HashSet<MechanismType>(allowedMechanisms);
            AllowedGlyphs = new HashSet<GlyphType>(allowedGlyphs);
        }
    }
}
