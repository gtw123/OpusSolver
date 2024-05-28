using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    /// <summary>
    /// Assembles multiple monoatomic molecules.
    /// </summary>
    public class MonoatomicAssembler : MoleculeAssembler
    {
        public override Vector2 OutputPosition => new Vector2();

        public const int MaxProducts = 4;

        private Dictionary<int, Arm> m_outputArms = new Dictionary<int, Arm>();

        public MonoatomicAssembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer)
        {
            if (products.Any(r => r.Atoms.Count() > 1))
            {
                throw new ArgumentException($"{nameof(MonoatomicAssembler)} can't handle products with multiple atoms.");
            }

            if (products.Count() == 1)
            {
                throw new ArgumentException($"{nameof(MonoatomicAssembler)} should not be used with only one reagent.");
            }

            if (products.Count() > MaxProducts)
            {
                throw new ArgumentException(Invariant($"{nameof(MonoatomicAssembler)} can't handle more than {MaxProducts} products."));
            }

            // We position one product directly at (0, 0). To minimise cycles, this should be the product
            // that's built last, so we reverse the order of the products here.
            products = products.Reverse();
            new Product(this, new Vector2(0, 0), HexRotation.R0, products.First());
            var dir = HexRotation.R0;
            foreach (var product in products.Skip(1))
            {
                CreateOutput(product, dir);
                dir -= HexRotation.R120;
            }
        }

        private void CreateOutput(Molecule product, HexRotation direction)
        {
            var pos = new Vector2(0, 0).OffsetInDirection(direction, 1);
            new Product(this, pos, HexRotation.R0, product);
            m_outputArms[product.ID] = new Arm(this, pos.Rotate60Clockwise(), direction + HexRotation.R120, ArmType.Arm1, extension: 1);
        }

        public override IEnumerable<Element> GetProductElementOrder(Molecule product)
        {
            return [product.Atoms.Single().Element];
        }

        public override void AddAtom(Element element, int productID)
        {
            // The last product is already positioned at (0, 0) so doesn't need any instructions
            if (m_outputArms.TryGetValue(productID, out var arm))
            {
                Writer.WriteGrabResetAction(arm, Instruction.RotateClockwise);
            }
        }
    }
}
