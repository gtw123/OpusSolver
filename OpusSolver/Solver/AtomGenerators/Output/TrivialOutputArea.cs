using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// A trivial output area used when there is only one product and it has a single atom.
    /// </summary>
    public class TrivialOutputArea : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        public TrivialOutputArea(ProgramWriter writer, IEnumerable<Molecule> products)
            : base(writer)
        {
            var product = products.Single();
            new Product(this, new Vector2(), HexRotation.R0, product);
        }
    }
}
