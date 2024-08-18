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
        public const int MaxProducts = 2;

        private Dictionary<int, Product> m_outputs = new();

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

            // We position one product directly at (0, 0). To minimise cycles, this should be the product
            // that's built last, so we reverse the order of the products here.
            products = products.Reverse();
            var product = products.First();
            m_outputs[product.ID] = new Product(this, new Vector2(0, 0), HexRotation.R0, product);

            product = products.Skip(1).SingleOrDefault();
            if (product != null)
            {
                var pos = new Vector2(ArmArea.ArmLength, 0).RotateBy(HexRotation.R120);
                m_outputs[product.ID] = new Product(this, pos, HexRotation.R60, product);
            }
        }

        public override void AddAtom(Element element, int productID)
        {
            var output = m_outputs[productID];
            var targetRotation = output.Transform.Rotation;
            ArmArea.RotateArmTo(GetWorldTransform().Rotation + targetRotation);
            Writer.Write(ArmArea.MainArm, Instruction.Drop);
        }
    }
}
