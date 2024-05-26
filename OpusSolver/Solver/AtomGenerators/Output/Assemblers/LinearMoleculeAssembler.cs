using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    public class LinearMoleculeAssembler : MoleculeAssembler
    {
        public override Vector2 OutputPosition => m_outputPosition;

        private readonly IEnumerable<Molecule> m_products;
        private readonly LoopingCoroutine<object> m_assembleCoroutine;
        private readonly Arm m_arm;
        private ProductConveyor m_productConveyor;

        private Vector2 m_outputPosition;
        private Molecule m_currentProduct;

        public LinearMoleculeAssembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer, parent.OutputPosition)
        {
            if (products.Any(p => p.Height != 1))
            {
                throw new ArgumentException("LinearMoleculeAssembler only works with linear molecules that are orientated horizontally.");
            }

            if (products.Any(r => r.HasTriplex))
            {
                throw new ArgumentException("LinearMoleculeAssembler doesn't work with triplex bonds.");
            }

            m_products = products;
            m_assembleCoroutine = new LoopingCoroutine<object>(Assemble);

            new Glyph(this, new Vector2(0, 0), HexRotation.R0, GlyphType.Bonding);
            m_arm = new Arm(this, new Vector2(0, -1), HexRotation.R60, ArmType.Piston, 1);

            if (products.Count() == 1)
            {
                new Track(this, m_arm.Transform.Position, HexRotation.R0, 1);
                m_outputPosition = new Vector2(0, 1);
            }
            else
            {
                // We need to shuffle the products along a bit to avoid hitting the output arm of the parent component
                new Track(this, m_arm.Transform.Position, HexRotation.R0, 3);
                m_outputPosition = new Vector2(2, 0);
            }

            m_productConveyor = new ProductConveyor(this, writer, m_products);
        }

        public override void AddAtom(Element element, int productID)
        {
            m_currentProduct = m_products.Single(product => product.ID == productID);
            m_assembleCoroutine.Next();
        }

        private IEnumerable<object> Assemble()
        {
            for (int x = m_currentProduct.Width - 1; x >= 0; x--)
            {
                if (x > 0)
                {
                    Writer.WriteGrabResetAction(m_arm, Instruction.MovePositive);
                    yield return null;
                }
                else
                {
                    if (m_products.Count() == 1)
                    {
                        Writer.WriteGrabResetAction(m_arm, Instruction.Extend);
                    }
                    else
                    {
                        Writer.WriteGrabResetAction(m_arm, [Instruction.MovePositive, Instruction.MovePositive]);
                    }

                    m_productConveyor.MoveProductToOutputLocation(m_currentProduct);

                    yield return null;
                }
            }
        }
    }
}
