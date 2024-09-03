﻿using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output.Complex
{
    public class ComplexAssembler : MoleculeAssembler
    {
        private readonly Dictionary<int, LoopingCoroutine<object>> m_assembleCoroutines;

        private class Output
        {
            public Product ProductGlyph;
            public Transform2D GrabberTransform;
            public bool DoExtraPivot;
        }

        private readonly Dictionary<int, Output> m_outputs = new();
        private Glyph m_bonder;

        public override int RequiredWidth => 2;

        private static readonly Transform2D LowerBonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D UpperBonderPosition = new Transform2D(new Vector2(-1, 1), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [LowerBonderPosition, UpperBonderPosition];

        public ComplexAssembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, IEnumerable<MoleculeBuilder> builders)
            : base(parent, writer, armArea)
        {
            m_assembleCoroutines = builders.ToDictionary(b => b.Product.ID, b => new LoopingCoroutine<object>(() => Assemble(b)));
            m_bonder = new Glyph(this, LowerBonderPosition.Position, HexRotation.R120, GlyphType.Bonding);

            AddOutputs(builders);
        }

        private void AddOutputs(IEnumerable<MoleculeBuilder> builders)
        {
            if (builders.Count() > 2)
            {
                throw new SolverException("ComplexAssembler currently only supports two products.");
            }

            var rotationFromBonderToOutput = HexRotation.R60 * builders.Count();
            bool doExtraPivot = false;
            foreach (var builder in builders.Reverse())
            {
                var product = builder.Product;

                // Calculate the final rotation of the molecule
                var finalOp = builder.Operations.Last();
                var moleculeTransform = GetMoleculeTransform(finalOp.MoleculeRotation);

                // If we're doing an extra pivot for one molecule then we need to do it for all subsequent ones too
                // to avoid overlap
                if (!doExtraPivot)
                {
                    // TODO: Use a better condition to decide whether to do this extra pivot
                    doExtraPivot = product.Height == 1;
                }

                if (doExtraPivot)
                {
                    // Do an extra pivot to help avoid hitting reagents in a counterclockwise direction
                    moleculeTransform.Rotation -= HexRotation.R60;
                }

                var outputPos = moleculeTransform.Apply(-finalOp.Atom.Position);
                var armPos = UpperBonderPosition.Position - new Vector2(ArmArea.ArmLength, 0);
                outputPos = outputPos.RotateAbout(armPos, rotationFromBonderToOutput);
                var productGlyph = new Product(this, outputPos, moleculeTransform.Rotation + rotationFromBonderToOutput, product);

                var grabberPosition = UpperBonderPosition.Position.RotateAbout(armPos, rotationFromBonderToOutput);
                var grabberTransform = new Transform2D(grabberPosition, rotationFromBonderToOutput);

                m_outputs[product.ID] = new Output { ProductGlyph = productGlyph, GrabberTransform = grabberTransform, DoExtraPivot = doExtraPivot };

                rotationFromBonderToOutput = rotationFromBonderToOutput.Rotate60Clockwise();
            }
        }

        public override void AddAtom(Element element, int productID)
        {
            m_assembleCoroutines[productID].Next();
        }

        private Transform2D GetMoleculeTransform(HexRotation moleculeRotation)
        {
            return new Transform2D(UpperBonderPosition.Position, moleculeRotation + m_bonder.Transform.Rotation - HexRotation.R180);
        }

        private IEnumerable<object> Assemble(MoleculeBuilder builder)
        {
            var placedAtoms = new List<Atom>();

            var operations = builder.Operations;
            for (int opIndex = 0; opIndex < operations.Count; opIndex++)
            {
                var op = operations[opIndex];

                ArmArea.MoveGrabberTo(LowerBonderPosition, this);

                if (placedAtoms.Any())
                {
                    GridState.UnregisterMolecule(placedAtoms.Last().Position, GetMoleculeTransform(op.MoleculeRotation), placedAtoms, this);
                }

                ArmArea.MoveGrabberTo(UpperBonderPosition, this);

                if (opIndex == operations.Count - 1)
                {
                    var output = m_outputs[builder.Product.ID];
                    if (output.DoExtraPivot)
                    {
                        ArmArea.PivotClockwise();
                    }

                    ArmArea.MoveGrabberTo(output.GrabberTransform, this);
                }
                else
                {
                    ArmArea.Pivot(op.RotationToNext);
                    placedAtoms.Add(op.Atom);
                    GridState.RegisterMolecule(placedAtoms.Last().Position, GetMoleculeTransform(op.MoleculeRotation + op.RotationToNext), placedAtoms, this);
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
