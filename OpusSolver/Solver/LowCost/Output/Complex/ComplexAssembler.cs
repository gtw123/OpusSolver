﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output.Complex
{
    public class ComplexAssembler : MoleculeAssembler
    {
        private IEnumerable<MoleculeBuilder> m_builders;
        private readonly Dictionary<int, LoopingCoroutine<object>> m_assembleCoroutines;

        private class Output
        {
            public Molecule Product;
            public Transform2D OutputTransform;
            public Transform2D GrabberTransform;
            public HexRotation Pivot;
        }

        private readonly Dictionary<int, Output> m_outputs = new();
        private Glyph m_bonder;

        public override int RequiredWidth => 2;

        private static readonly Transform2D LowerBonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D UpperBonderPosition = new Transform2D(new Vector2(-1, 1), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [LowerBonderPosition, UpperBonderPosition];

        /// <summary>
        /// The direction in which this assembler's bonder creates bonds, from the previous atom to the new atom.
        /// </summary>
        public static HexRotation BondingDirection = HexRotation.R300;

        public ComplexAssembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, IEnumerable<MoleculeBuilder> builders)
            : base(parent, writer, armArea)
        {
            m_builders = builders;
            m_assembleCoroutines = builders.ToDictionary(b => b.Product.ID, b => new LoopingCoroutine<object>(() => Assemble(b)));
            m_bonder = new Glyph(this, LowerBonderPosition.Position, BondingDirection - HexRotation.R180, GlyphType.Bonding);
        }

        public override void BeginSolution()
        {
            // Create the outputs here so that everything else will be created first and we can place
            // the outputs without overlapping other parts
            AddOutputs();
        }

        private void AddOutputs()
        {
            if (m_builders.Count() > 2)
            {
                throw new UnsupportedException("ComplexAssembler currently only supports two products.");
            }

            var possibleRotations = new[] { HexRotation.R60, HexRotation.R120 };
            var possiblePivots = new[] { HexRotation.R0, -HexRotation.R60 };
            var rotationCases = possibleRotations.SelectMany(r => possiblePivots, (rotation, pivot) => (rotation, pivot)).ToList();

            var builder1 = m_builders.First();
            foreach (var (rot1, pivot1) in rotationCases)
            {
                var output1 = CalculateOutputTransform(builder1, rot1, pivot1);

                // Check that none of the output atoms overlap any other atoms
                var atomPositions = builder1.Product.GetTransformedAtomPositions(GetWorldTransform().Apply(output1.OutputTransform));
                if (atomPositions.Any(p => GridState.GetAtom(p) != null))
                {
                    continue;
                }

                if (m_builders.Count() == 1)
                {
                    m_outputs[builder1.Product.ID] = output1;
                    break;
                }

                var builder2 = m_builders.Skip(1).Single();
                foreach (var (rot2, pivot2) in rotationCases.Where(c => c.rotation != rot1))
                {
                    var output2 = CalculateOutputTransform(builder2, rot2, pivot2);

                    // Check that none of the output atoms overlap any other atoms
                    var atomPositions2 = builder2.Product.GetTransformedAtomPositions(GetWorldTransform().Apply(output2.OutputTransform));
                    if (atomPositions2.Any(p => GridState.GetAtom(p) != null))
                    {
                        continue;
                    }

                    // Check that the outputs don't overlap with each other
                    if (atomPositions.Intersect(atomPositions2).Any())
                    {
                        continue;
                    }

                    m_outputs[builder1.Product.ID] = output1;
                    m_outputs[builder2.Product.ID] = output2;
                    break;
                }
            }

            var missingOutputs = m_outputs.Keys.Except(m_builders.Select(b => b.Product.ID));
            if (missingOutputs.Any())
            {
                throw new InvalidOperationException($"Could not find valid output positions for products.");
            }

            foreach (var (id, output) in m_outputs)
            {
                new Product(this, output.OutputTransform.Position, output.OutputTransform.Rotation, output.Product);
            }
        }

        private Output CalculateOutputTransform(MoleculeBuilder builder, HexRotation rotationFromBonderToOutput, HexRotation pivotToOutput)
        {
            var finalOp = builder.Operations.Last();
            var moleculeTransform = new Transform2D(UpperBonderPosition.Position, finalOp.MoleculeRotation + pivotToOutput);
            moleculeTransform = moleculeTransform.Apply(new Transform2D(-finalOp.Atom.Position, HexRotation.R0));

            var armPos = UpperBonderPosition.Position - new Vector2(ArmArea.ArmLength, 0);
            var outputTransform = moleculeTransform.RotateAbout(armPos, rotationFromBonderToOutput);

            var grabberTransform = UpperBonderPosition.RotateAbout(armPos, rotationFromBonderToOutput);

            return new Output { Product = builder.Product, OutputTransform = outputTransform, GrabberTransform = grabberTransform, Pivot = pivotToOutput };
        }

        public override void AddAtom(Element element, int productID)
        {
            m_assembleCoroutines[productID].Next();
        }

        private IEnumerable<object> Assemble(MoleculeBuilder builder)
        {
            var assembledAtoms = new AtomCollection();
            bool isBondingUpsideDown = false;

            var operations = builder.Operations;
            for (int opIndex = 0; opIndex < operations.Count; opIndex++)
            {
                var op = operations[opIndex];

                if (isBondingUpsideDown)
                {
                    ArmArea.MoveGrabberTo(LowerBonderPosition, this, armRotationOffset: HexRotation.R120);
                    ArmArea.DropAtom();

                    // TODO: Register the dropped atom

                    // Grab the previously dropped atoms
                    ArmArea.MoveGrabberTo(UpperBonderPosition, this, armRotationOffset: HexRotation.R120);
                    ArmArea.GrabAtom(assembledAtoms.Atoms.Last().Element);
                    GridState.UnregisterAtoms(assembledAtoms, this);

                    // Move them down to the lower bonder position
                    ArmArea.MoveGrabberTo(UpperBonderPosition, this);
                    // TODO: Use grabber position here?
                    assembledAtoms.Transform = assembledAtoms.Transform.RotateAbout(UpperBonderPosition.Position - new Vector2(ArmArea.ArmLength, 0), -HexRotation.R120);

                    ArmArea.MoveGrabberTo(LowerBonderPosition, this);
                    assembledAtoms.Transform.Position = LowerBonderPosition.Position;

                    var targetRotation = op.MoleculeRotation + HexRotation.R180;
                    var requiredPivot = targetRotation - assembledAtoms.Transform.Rotation;
                    ArmArea.PivotBy(requiredPivot, rotateClockwiseIf180Degrees: true);
                    assembledAtoms.Transform = assembledAtoms.Transform.RotateAbout(LowerBonderPosition.Position, requiredPivot);

                    ArmArea.DropAtom();
                    GridState.RegisterAtoms(assembledAtoms, this);

                    // Grab the previously dropped atom and move it to the upper bonder position
                    ArmArea.MoveGrabberTo(LowerBonderPosition, this, armRotationOffset: HexRotation.R120);
                    ArmArea.GrabAtom(op.Atom.Element);
                    ArmArea.MoveGrabberTo(UpperBonderPosition, this);
                    GridState.UnregisterAtoms(assembledAtoms, this);
                    assembledAtoms.Transform.Position = UpperBonderPosition.Position;
                }
                else
                {
                    ArmArea.MoveGrabberTo(LowerBonderPosition, this);
                    GridState.UnregisterAtoms(assembledAtoms, this);
                    ArmArea.MoveGrabberTo(UpperBonderPosition, this);
                }

                if (opIndex == operations.Count - 1)
                {
                    var output = m_outputs[builder.Product.ID];

                    var targetRotation = op.MoleculeRotation + output.Pivot;
                    var requiredPivot = targetRotation - assembledAtoms.Transform.Rotation;

                    ArmArea.PivotBy(requiredPivot);
                    ArmArea.MoveGrabberTo(output.GrabberTransform, this);
                }
                else
                {
                    assembledAtoms.AddAtom(op.Atom);
                    assembledAtoms.LocalOrigin = op.Atom.Position;

                    if (opIndex == 0)
                    {
                        assembledAtoms.Transform = new Transform2D(UpperBonderPosition.Position, op.MoleculeRotation);
                    }

                    var targetRotation = op.MoleculeRotation + op.RotationToNext;
                    var requiredPivot = targetRotation - assembledAtoms.Transform.Rotation;

                    bool willCollide = false;
                    if (op.RotationToNext == HexRotation.R60 || op.RotationToNext == HexRotation.R120)
                    {
                        // TODO: Should we always do this CCW?
                        var currentRot = assembledAtoms.Transform.Rotation;
                        var rots = currentRot.CalculateRotationsTo(targetRotation);
                        willCollide = rots.Any(rot => GridState.WillAtomsCollideWhileRotating(assembledAtoms, UpperBonderPosition.Position, rot - currentRot, this));
                    }

                    if (willCollide)
                    {
                        // Rotate/pivot the molecule so that we can move the next atom behind it
                        var targetRotation2 = op.MoleculeRotation - HexRotation.R120;
                        var requiredPivot2 = targetRotation2 - assembledAtoms.Transform.Rotation;

                        ArmArea.PivotBy(requiredPivot2);
                        assembledAtoms.Transform = assembledAtoms.Transform.RotateAbout(UpperBonderPosition.Position, requiredPivot2);
                        ArmArea.MoveGrabberTo(UpperBonderPosition, this, armRotationOffset: HexRotation.R120);
                        assembledAtoms.Transform = assembledAtoms.Transform.RotateAbout(UpperBonderPosition.Position - new Vector2(ArmArea.ArmLength, 0), HexRotation.R120);
                        GridState.RegisterAtoms(assembledAtoms, this);

                        isBondingUpsideDown = true;
                    }
                    else
                    {
                        ArmArea.PivotBy(requiredPivot);
                        assembledAtoms.Transform = assembledAtoms.Transform.RotateAbout(UpperBonderPosition.Position, requiredPivot);
                        GridState.RegisterAtoms(assembledAtoms, this);

                        isBondingUpsideDown = false;
                    }
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
