﻿using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// An output area which assembles arbitrary products and moves them to their output locations.
    /// </summary>
    public class ComplexOutputArea : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        private IEnumerable<Molecule> m_products;
        private LoopingCoroutine<object> m_assembleCoroutine;

        private UniversalMoleculeAssembler m_assembler;
        private ProductConveyor m_productConveyor;

        private Molecule m_currentProduct;
        private int m_currentArm;

        public ComplexOutputArea(ProgramWriter writer, IEnumerable<Molecule> products)
            : base(writer)
        {
            m_products = products;
            m_assembleCoroutine = new LoopingCoroutine<object>(Assemble);

            m_assembler = new UniversalMoleculeAssembler(this, writer, products);
            m_productConveyor = new ProductConveyor(m_assembler, writer, products);
        }

        public override void Consume(Element element, int id)
        {
            m_currentProduct = m_products.Single(product => product.ID == id);
            m_assembleCoroutine.Next();
        }

        private IEnumerable<object> Assemble()
        {
            for (int y = m_currentProduct.Height - 1; y >= 0; y--)
            {
                m_currentArm = m_assembler.Width - 1;
                var atoms = m_currentProduct.GetRow(y).OrderByDescending(a => a.Position.X);
                foreach (var atom in atoms)
                {
                    GrabAtom(atom);

                    if (atom == atoms.Last())
                    {
                        FinishRow(y);

                        if (y == 0)
                        {
                            m_productConveyor.MoveProductToOutputLocation(m_currentProduct);
                        }
                    }

                    yield return null;
                }
            }
        }

        private void GrabAtom(Atom atom)
        {
            var arm = m_assembler.LowerArms[m_currentArm];
            if (atom.Bonds[Direction.E] == BondType.Single)
            {
                // No need to grab as the atom will have just been bonded to the
                // existing atom on the bonder
                m_assembler.SetUsedBonders(GlyphType.Bonding, Direction.E, true);
            }
            else
            {
                int distance = (m_assembler.Width - 1) - m_currentArm;
                Writer.AdjustTime(-distance);
                Writer.Write(arm, Enumerable.Repeat(Instruction.MovePositive, distance));
                Writer.Write(arm, Instruction.Grab);
            }

            // Move the atom to the rightmost side of the bonder
            Writer.Write(arm, Instruction.MovePositive);

            if (atom.Bonds[Direction.W] != BondType.Single)
            {
                // Move the atom to the assembly area
                Writer.Write(arm, Enumerable.Repeat(Instruction.MovePositive, atom.Position.X + 1));
                m_currentArm--;
            }
        }

        private void FinishRow(int row)
        {
            var activeArms = m_assembler.LowerArms.GetRange(m_currentArm + 1, m_assembler.Width - (m_currentArm + 1));

            if (row == m_currentProduct.Height - 1)
            {
                FinishFirstRow(row, activeArms);
            }
            else
            {
                FinishOtherRow(row, activeArms);
            }
        }

        private void FinishFirstRow(int row, IEnumerable<Arm> activeArms)
        {
            (var bondInstructions, var returnInstructions) = new BondProgrammer(m_assembler, m_currentProduct, row).Generate();

            if (row == 0 && !bondInstructions.Any())
            {
                // Simple case: single-height molecule with no special bonds. Just drop it straight on the output location.
                Writer.Write(activeArms, new[] { Instruction.Extend, Instruction.Reset });
                return;
            }

            Writer.Write(activeArms, Instruction.Reset);
            Writer.AdjustTime(-2);
            Writer.Write(m_assembler.UpperArms, new[] { Instruction.Retract, Instruction.Grab, Instruction.Extend });

            if (!bondInstructions.Any())
            {
                Writer.Write(m_assembler.UpperArms, Instruction.Extend);
                return;
            }

            Writer.Write(m_assembler.UpperArms, bondInstructions);
            Writer.Write(m_assembler.UpperArms, returnInstructions);

            if (row == 0)
            {
                // For a single-height molecule we can drop it as soon as the upper arms have returned.
                Writer.Write(m_assembler.UpperArms, Instruction.Drop);
            }
            else
            {
                Writer.Write(m_assembler.UpperArms, Instruction.Extend);
            }
        }

        private void FinishOtherRow(int row, IEnumerable<Arm> activeArms)
        {
            Writer.Write(activeArms, Instruction.Extend);

            (var bondInstructions, var returnInstructions) = new BondProgrammer(m_assembler, m_currentProduct, row).Generate();
            Writer.Write(activeArms.Concat(m_assembler.UpperArms), bondInstructions);

            // We don't need to return the lower arms before resetting them, so save some instructions by just doing a reset
            Writer.Write(activeArms, Instruction.Reset, updateTime: false);

            if (row > 0)
            {
                // Drop the molecule and re-grab it from the lower atoms. This is to ensure everything is connected.
                Writer.Write(m_assembler.UpperArms, new[] { Instruction.Drop, Instruction.Retract, Instruction.Grab });
                Writer.Write(m_assembler.UpperArms, returnInstructions);
                Writer.Write(m_assembler.UpperArms, Instruction.Extend);
            }
            else
            {
                // Last row - no need to re-grab
                Writer.Write(m_assembler.UpperArms, returnInstructions);
                Writer.Write(m_assembler.UpperArms, Instruction.Reset);
            }
        }

        public override void OptimizeParts()
        {
            m_assembler.OptimizeParts();
        }
    }
}
