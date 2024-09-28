using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.LowCost.Output
{
    /// <summary>
    /// Assembles multiple monoatomic molecules.
    /// </summary>
    public class MonoatomicAssembler : MoleculeAssembler
    {
        public const int MaxProducts = 4;

        private readonly Dictionary<int, Product> m_outputs = new();

        public override IEnumerable<Transform2D> RequiredAccessPoints => m_outputs.Values.OrderBy(o => o.Molecule.ID).Select(p => p.Transform);

        public MonoatomicAssembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, IEnumerable<Molecule> products)
            : base(parent, writer, armArea)
        {
            if (products.Any(r => r.Atoms.Count() > 1))
            {
                throw new ArgumentException($"{nameof(MonoatomicAssembler)} can't handle products with multiple atoms.");
            }

            if (products.Count() > MaxProducts)
            {
                throw new ArgumentException(Invariant($"{nameof(MonoatomicAssembler)} can't handle more than {MaxProducts} products."));
            }

            var productList = products.Reverse().ToList();
            AddProductOutput(productList[0], new Vector2(0, 0), HexRotation.R0);

            if (productList.Count > 1)
            {
                var pos = new Vector2(ArmArea.ArmLength, 0).RotateBy(HexRotation.R120);
                AddProductOutput(productList[1], pos, HexRotation.R60);
            }

            if (productList.Count > 2)
            {
                AddProductOutput(productList[2], new Vector2(-1, 0), HexRotation.R0);
            }

            if (productList.Count > 3)
            {
                var pos = new Vector2(ArmArea.ArmLength, 0).RotateBy(HexRotation.R120);
                AddProductOutput(productList[3], pos + new Vector2(-1, 0), HexRotation.R60);
            }
        }

        private void AddProductOutput(Molecule product, Vector2 position, HexRotation rotation)
        {
            var output = new Product(this, position, rotation, product);
            m_outputs[product.ID] = output;
        }

        public override void AddAtom(Element element, int productID)
        {
            var output = m_outputs[productID];
            ArmArea.MoveGrabberTo(output.Transform, this);
            ArmArea.DropAtoms();
        }
    }
}
