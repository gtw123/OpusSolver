using System.Collections.Generic;
using System.Linq;

namespace OpusSolver
{
    public class Puzzle
    {
        public string FileName { get; private set; }
        public string Name { get; private set; }
        public List<Molecule> Products { get; private set; }
        public List<Molecule> Reagents { get; private set; }
        public HashSet<ArmType> AllowedArmTypes { get; private set; }
        public HashSet<GlyphType> AllowedGlyphs { get; private set; }

        /// <summary>
        /// How many outputs are required for completion (a scale of 1 means 6 outputs).
        /// </summary>
        public int OutputScale { get; private set; }

        public Puzzle(string filename, string name, IEnumerable<Molecule> products, IEnumerable<Molecule> reagents, IEnumerable<ArmType> allowedArmTypes, IEnumerable<GlyphType> allowedGlyphs, int outputScale)
        {
            FileName = filename;
            Name = name;
            Products = products.ToList();
            Reagents = reagents.ToList();
            AllowedArmTypes = new HashSet<ArmType>(allowedArmTypes);
            AllowedGlyphs = new HashSet<GlyphType>(allowedGlyphs);
            OutputScale = outputScale;
        }
    }
}
