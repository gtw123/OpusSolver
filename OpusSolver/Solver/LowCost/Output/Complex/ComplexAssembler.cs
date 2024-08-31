﻿using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output.Complex
{
    public class ComplexAssembler : MoleculeAssembler
    {
        private readonly Dictionary<int, LoopingCoroutine<object>> m_assembleCoroutines;
        private readonly Dictionary<int, Product> m_outputs = new();
        private Glyph m_bonder;

        public override int RequiredWidth => 2;

        private static readonly Transform2D LowerBonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D UpperBonderPosition = new Transform2D(new Vector2(-1, 1), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [LowerBonderPosition, UpperBonderPosition];

        // TODO: Remove this?
        private bool m_doExtraPivot = false;

        public ComplexAssembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, IEnumerable<MoleculeBuilder> builders)
            : base(parent, writer, armArea)
        {
            m_assembleCoroutines = builders.ToDictionary(b => b.Product.ID, b => new LoopingCoroutine<object>(() => Assemble(b)));
            m_bonder = new Glyph(this, LowerBonderPosition.Position, HexRotation.R120, GlyphType.Bonding);

            AddOutputs(builders);
        }

        private void AddOutputs(IEnumerable<MoleculeBuilder> builders)
        {
            if (builders.Count() > 1)
            {
                throw new SolverException("ComplexAssembler currently only supports one product.");
            }

            var builder = builders.First();
            var product = builder.Product;

            // Calculate the final rotation of the molecule taking into account the rotation of the bonder
            var finalOp = builder.Operations.Last();
            var finalRotation = finalOp.MoleculeRotation + m_bonder.Transform.Rotation - HexRotation.R180;

            if (m_doExtraPivot)
            {
                // Do an extra pivot to help avoid hitting reagents in a counterclockwise direction
                finalRotation -= HexRotation.R60;
            }

            var pos = UpperBonderPosition.Position - finalOp.Atom.Position.RotateBy(finalRotation);
            var armPos = UpperBonderPosition.Position - new Vector2(ArmArea.ArmLength, 0);
            var rotationFromBonderToOutput = HexRotation.R60;
            pos = pos.RotateAbout(armPos, rotationFromBonderToOutput);

            var transform = new Transform2D(pos, finalRotation + rotationFromBonderToOutput);
            var output = new Product(this, transform.Position, transform.Rotation, product);
            m_outputs[product.ID] = output;
        }

        public override void AddAtom(Element element, int productID)
        {
            m_assembleCoroutines[productID].Next();
        }

        private IEnumerable<object> Assemble(MoleculeBuilder builder)
        {
            var operations = builder.Operations;
            for (int opIndex = 0; opIndex < operations.Count; opIndex++)
            {
                ArmArea.MoveGrabberTo(this, LowerBonderPosition);
                ArmArea.MoveGrabberTo(this, UpperBonderPosition);

                if (opIndex == operations.Count - 1)
                {
                    if (m_doExtraPivot)
                    {
                        ArmArea.PivotClockwise();
                    }

                    var armPos = UpperBonderPosition.Position - new Vector2(ArmArea.ArmLength, 0);
                    var grabberPosition = UpperBonderPosition.Position.RotateAbout(armPos, HexRotation.R60);
                    ArmArea.MoveGrabberTo(this, new Transform2D(grabberPosition, HexRotation.R60));
                }
                else
                {
                    ArmArea.Pivot(operations[opIndex].RotationToNext);
                }

                ArmArea.DropAtom();
                yield return null;
            }
        }

        public static bool IsProductCompatible(Molecule product)
        {
            // For now, only allow molecules with no branches or loops
            return product.Atoms.All(a => a.BondCount <= 2) && product.Atoms.Count(a => a.BondCount == 1) == 2;
        }

        public static IEnumerable<MoleculeBuilder> CreateMoleculeBuilders(IEnumerable<Molecule> products)
        {
            return products.Select(p => new MoleculeBuilder(p)).ToList();
        }
    }
}
