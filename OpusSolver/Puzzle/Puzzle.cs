using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpusSolver
{
    public class Puzzle
    {
        public string FilePath { get; private set; }
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

        public Puzzle(string filePath, string name, IEnumerable<Molecule> products, IEnumerable<Molecule> reagents, IEnumerable<ArmType> allowedArmTypes, IEnumerable<GlyphType> allowedGlyphs, int outputScale)
        {
            FilePath = filePath;
            FileName = Path.GetFileNameWithoutExtension(filePath);
            Name = name;
            Products = products.ToList();
            Reagents = reagents.ToList();
            AllowedArmTypes = new HashSet<ArmType>(allowedArmTypes);
            AllowedGlyphs = new HashSet<GlyphType>(allowedGlyphs);
            OutputScale = outputScale;
        }

        public override string ToString()
        {
            var str = new StringBuilder();

            str.AppendLine($"Name: {Name}");

            str.AppendLine("Reagents:");
            foreach (var reagent in Reagents)
            {
                str.Append(reagent.ToString());
            }

            str.AppendLine("Products:");
            foreach (var product in Products)
            {
                str.Append(product.ToString());
            }

            str.AppendLine($"Allowed Arm Types: {string.Join(", ", AllowedArmTypes.OrderBy(t => t))}");
            str.AppendLine($"Allowed Glyphs: {string.Join(", ", AllowedGlyphs.OrderBy(g => g))}");
            str.AppendLine($"Output Scale: {OutputScale}");

            return str.ToString();
        }
    }
}
