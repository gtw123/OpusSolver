using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output.Complex
{
    public class ComplexAssembler : MoleculeAssembler
    {
        private IEnumerable<MoleculeBuilder> m_builders;
        private readonly Dictionary<int, LoopingCoroutine<object>> m_assembleCoroutines;
        private readonly Glyph m_bonder;

        private readonly Dictionary<int, Product> m_outputs = new();

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
                throw new UnsupportedException($"ComplexAssembler currently only supports 2 products (requested {m_builders.Count()}).");
            }

            var possibleRotations = new[] { HexRotation.R60, HexRotation.R120 };
            var possiblePivots = HexRotation.All;
            var possiblePositions = new[] { UpperBonderPosition.Position, LowerBonderPosition.Position };
            var rotationCases = (
                from rotation in possibleRotations
                from pivot in possiblePivots
                from position in possiblePositions
                select (rotation, pivot, position)).ToList();

            var outputTransforms = new Dictionary<int, Transform2D>();

            var builder1 = m_builders.First();
            foreach (var (rot1, pivot1, pos1) in rotationCases)
            {
                var output1Transform = CalculateOutputTransform(builder1, rot1, pivot1, pos1);

                // Check that none of the output atoms overlap any other atoms
                var atomPositions = builder1.Product.GetTransformedAtomPositions(GetWorldTransform().Apply(output1Transform));
                if (atomPositions.Any(p => GridState.CellContainsAnyObject(p)))
                {
                    continue;
                }

                if (m_builders.Count() == 1)
                {
                    outputTransforms[builder1.Product.ID] = output1Transform;
                    break;
                }

                var builder2 = m_builders.Skip(1).Single();
                foreach (var (rot2, pivot2, pos2) in rotationCases)
                {
                    var output2Transform = CalculateOutputTransform(builder2, rot2, pivot2, pos2);

                    // Check that none of the output atoms overlap any other atoms
                    var atomPositions2 = builder2.Product.GetTransformedAtomPositions(GetWorldTransform().Apply(output2Transform));
                    if (atomPositions2.Any(p => GridState.CellContainsAnyObject(p)))
                    {
                        continue;
                    }

                    // Check that the outputs don't overlap with each other
                    if (atomPositions.Intersect(atomPositions2).Any())
                    {
                        continue;
                    }

                    outputTransforms[builder1.Product.ID] = output1Transform;
                    outputTransforms[builder2.Product.ID] = output2Transform;
                    break;
                }
            }

            var missingOutputs = m_builders.Select(b => b.Product.ID).Except(outputTransforms.Keys);
            if (missingOutputs.Any())
            {
                throw new SolverException($"Could not find valid output positions for products.");
            }

            foreach (var builder in m_builders)
            {
                var outputTransform = outputTransforms[builder.Product.ID];
                m_outputs[builder.Product.ID] = new Product(this, outputTransform.Position, outputTransform.Rotation, builder.Product);
            }
        }

        private Transform2D CalculateOutputTransform(MoleculeBuilder builder, HexRotation rotationFromBonderToOutput, HexRotation pivotToOutput, Vector2 position)
        {
            var finalOp = builder.Operations.Last();
            var moleculeTransform = new Transform2D(position, finalOp.MoleculeRotation + pivotToOutput);
            moleculeTransform = moleculeTransform.Apply(new Transform2D(-finalOp.Atom.Position, HexRotation.R0));

            var armPos = position - new Vector2(ArmArea.ArmLength, 0);
            return moleculeTransform.RotateAbout(armPos, rotationFromBonderToOutput);
        }

        public override void AddAtom(Element element, int productID)
        {
            m_assembleCoroutines[productID].Next();
        }

        private IEnumerable<object> Assemble(MoleculeBuilder builder)
        {
            AtomCollection assembledAtoms = null;

            bool isBondingUpsideDown = false;

            var operations = builder.Operations;
            for (int opIndex = 0; opIndex < operations.Count; opIndex++)
            {
                var op = operations[opIndex];

                if (isBondingUpsideDown)
                {
                    // Stash the new atom "behind" the previously dropped atoms
                    ArmController.MoveAtomsTo(ArmController.GetRotatedGrabberTransform(LowerBonderPosition, HexRotation.R120), this);
                    var newAtom = ArmController.DropAtoms();

                    // Grab the previously dropped atoms
                    ArmController.MoveGrabberTo(UpperBonderPosition, this, armRotationOffset: HexRotation.R120);
                    ArmController.GrabAtoms(assembledAtoms);

                    // Move them down to the lower bonder position and rotate them to prepare for the bond
                    var targetMoleculeTransform = ArmController.GetAtomsTransformForGrabberTransform(LowerBonderPosition, this);
                    var targetRotation = op.MoleculeRotation + HexRotation.R180;
                    var requiredPivot = targetRotation - targetMoleculeTransform.Rotation;
                    targetMoleculeTransform = targetMoleculeTransform.RotateAbout(GetWorldTransform().Apply(LowerBonderPosition.Position), requiredPivot);
                    ArmController.MoveAtomsTo(targetMoleculeTransform);
                    ArmController.DropAtoms();

                    // Grab the previously dropped new atom and move it to the upper bonder position
                    ArmController.MoveGrabberTo(LowerBonderPosition, this, armRotationOffset: HexRotation.R120);
                    ArmController.GrabAtoms(newAtom);

                    // Bond it to the other atoms
                    ArmController.MoveAtomsTo(ArmController.GetAtomsTransformForGrabberTransform(UpperBonderPosition, this), options: new ArmMovementOptions { AllowExternalBonds = true });
                    ArmController.BondAtomsTo(assembledAtoms, m_bonder);
                }
                else
                {
                    if (opIndex == 0)
                    {
                        ArmController.MoveAtomsTo(UpperBonderPosition, this);
                        assembledAtoms = ArmController.GrabbedAtoms;

                        // Adjust the atom position so it matches the corresponding target molecule, but also update
                        // the transform so that the atom has the same world transform as before.
                        // TODO: Can we simplify this calculation?
                        assembledAtoms.WorldTransform.Position = assembledAtoms.WorldTransform.Apply(assembledAtoms.Atoms[0].Position) - op.Atom.Position.RotateBy(op.MoleculeRotation);
                        assembledAtoms.WorldTransform.Rotation = op.MoleculeRotation;
                        assembledAtoms.Atoms[0].Position = op.Atom.Position;
                    }
                    else
                    {
                        ArmController.MoveAtomsTo(LowerBonderPosition, this, options: new ArmMovementOptions { AllowExternalBonds = true });
                        ArmController.BondAtomsTo(assembledAtoms, m_bonder);

                        if (opIndex != operations.Count - 1)
                        {
                            // Move the grabbed atom up to make way for the next atom to bond.
                            ArmController.MoveGrabberTo(UpperBonderPosition, this);
                        }
                    }
                }

                if (opIndex == operations.Count - 1)
                {
                    ArmController.MoveAtomsTo(m_outputs[builder.Product.ID].Transform, this);
                    ArmController.DropAtoms(addToGrid: false);
                }
                else
                {
                    var targetRotation = op.MoleculeRotation + op.RotationToNext;
                    if (ArmController.TryPivotBy(targetRotation - assembledAtoms.WorldTransform.Rotation))
                    {
                        isBondingUpsideDown = false;
                    }
                    else
                    {
                        // Rotate/pivot the molecule so that we can move the next atom behind it
                        var targetRotation2 = op.MoleculeRotation - HexRotation.R120;
                        var requiredPivot = targetRotation2 - assembledAtoms.WorldTransform.Rotation;
                        var targetTransform = assembledAtoms.WorldTransform.RotateAbout(ArmController.GetGrabberPosition(), requiredPivot);
                        targetTransform = targetTransform.RotateAbout(ArmController.ArmTransform.Position, HexRotation.R120);
                        ArmController.MoveAtomsTo(targetTransform);
                        isBondingUpsideDown = true;
                    }

                    ArmController.DropAtoms();
                }

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
