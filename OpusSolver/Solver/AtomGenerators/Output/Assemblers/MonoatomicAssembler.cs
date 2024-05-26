using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    public class MonoatomicAssembler : MoleculeAssembler
    {
        public override Vector2 OutputPosition => new Vector2();

        public const int MaxProducts = 4;

        private Dictionary<int, Arm> m_outputArms = new Dictionary<int, Arm>();

        public MonoatomicAssembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer, parent.OutputPosition)
        {
            if (products.Any(r => r.Atoms.Count() > 1))
            {
                throw new ArgumentException($"{nameof(MonoatomicAssembler)} can't handle products with multiple atoms.");
            }

            if (products.Count() > MaxProducts)
            {
                throw new ArgumentException(Invariant($"{nameof(MonoatomicAssembler)} can't handle more than {MaxProducts} products."));
            }

            var dir = HexRotation.R0;
            foreach (var product in products)
            {
                CreateOutput(product, dir);
                dir = dir.Rotate60Clockwise();
            }
        }

        private void CreateOutput(Molecule product, HexRotation direction)
        {
            var pos = new Vector2(0, 0).OffsetInDirection(direction, 1);
            new Product(this, pos, HexRotation.R0, product);
            m_outputArms[product.ID] = new Arm(this, pos * 2, direction.Rotate180(), ArmType.Piston, extension: 2);
        }

        public override void AddAtom(Element element, int productID)
        {
            Writer.WriteGrabResetAction(m_outputArms[productID], Instruction.Retract);
        }
    }
}
