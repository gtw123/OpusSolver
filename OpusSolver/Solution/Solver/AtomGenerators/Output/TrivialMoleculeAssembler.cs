using System.Collections.Generic;
using System.Linq;

namespace Opus.Solution.Solver.AtomGenerators.Output
{
    /// <summary>
    /// A trivial assembler used when there is only one product and it has a single atom.
    /// </summary>
    public class TrivialMoleculeAssembler : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        public TrivialMoleculeAssembler(ProgramWriter writer, IEnumerable<Molecule> products)
            : base(writer)
        {
            var product = products.Single();
            new Product(this, new Vector2(), product.Rotation, product.ID);
        }
    }
}
