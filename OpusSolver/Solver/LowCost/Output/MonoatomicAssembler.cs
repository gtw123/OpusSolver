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

        // We need to manually specify the order in which to add the access points because the logic in ArmArea
        // for building the track is currently a bit simplistic.
        private readonly List<int> m_outputAccessPointOrder = new();
        public override IEnumerable<Transform2D> RequiredAccessPoints => m_outputAccessPointOrder.Select(o => m_outputs[o]).Select(p => p.Transform);

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
                AddProductOutput(productList[2], new Vector2(-1, 0), HexRotation.R0, addAccessPointAtStart: true);
            }

            if (productList.Count > 3)
            {
                var pos = new Vector2(ArmArea.ArmLength, 0).RotateBy(HexRotation.R120);
                AddProductOutput(productList[3], pos + new Vector2(-1, 0), HexRotation.R60);
            }
        }

        private void AddProductOutput(Molecule product, Vector2 position, HexRotation rotation, bool addAccessPointAtStart = false)
        {
            var output = new Product(this, position, rotation, product);
            m_outputs[product.ID] = output;

            if (addAccessPointAtStart)
            {
                m_outputAccessPointOrder.Insert(0, product.ID);
            }
            else
            {
                m_outputAccessPointOrder.Add(product.ID);
            }
        }

        public override void AddAtom(Element element, int productID)
        {
            var output = m_outputs[productID];
            ArmArea.MoveGrabberTo(this, output.Transform);
            ArmArea.DropAtom();
        }
    }
}
