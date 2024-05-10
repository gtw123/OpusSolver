﻿using System.Collections.Generic;
using System.Linq;

namespace OpusSolver
{
    public class Puzzle
    {
        public string FileName { get; private set; }
        public string Name { get; private set; }
        public List<Molecule> Products { get; private set; }
        public List<Molecule> Reagents { get; private set; }
        public HashSet<MechanismType> AllowedMechanisms { get; private set; }
        public HashSet<GlyphType> AllowedGlyphs { get; private set; }

        public Puzzle(string filename, string name, IEnumerable<Molecule> products, IEnumerable<Molecule> reagents, IEnumerable<MechanismType> allowedMechanisms, IEnumerable<GlyphType> allowedGlyphs)
        {
            FileName = filename;
            Name = name;
            Products = products.ToList();
            Reagents = reagents.ToList();
            AllowedMechanisms = new HashSet<MechanismType>(allowedMechanisms);
            AllowedGlyphs = new HashSet<GlyphType>(allowedGlyphs);
        }
    }
}
