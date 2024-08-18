using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard.Output
{
    /// <summary>
    /// Assembles a single monoatomic molecule.
    /// </summary>
    public class SingleMonoatomicAssembler : MoleculeAssembler
    {

        public SingleMonoatomicAssembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer)
        {
            var product = products.Single();
            new Product(this, new Vector2(), HexRotation.R0, product);
        }

        public override void AddAtom(Element element, int productID)
        {
            // There's nothing to do here since the atom will get placed directly onto the product output
        }
    }
}
