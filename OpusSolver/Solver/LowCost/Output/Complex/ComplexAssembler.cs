using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output.Complex
{
    public class ComplexAssembler : MoleculeAssembler
    {
        public const int MaxProducts = 4;

        private readonly IReadOnlyList<MoleculeBuilder> m_builders;
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
            m_builders = builders.ToList();
            m_assembleCoroutines = builders.ToDictionary(b => b.Product.ID, b => new LoopingCoroutine<object>(() => Assemble(b)));
            m_bonder = new Glyph(this, LowerBonderPosition.Position, BondingDirection - HexRotation.R180, GlyphType.Bonding);
        }

        public override void BeginSolution()
        {
            // Create the outputs here so that everything else will be created first and we can place
            // the outputs without overlapping other parts
            AddOutputs();
        }

        private record class OutputLocation(Transform2D Transform, List<Vector2> AtomPositions);

        private void AddOutputs()
        {
            if (m_builders.Count > MaxProducts)
            {
                throw new SolverException($"ComplexAssembler currently only supports {MaxProducts} products (requested {m_builders.Count}).");
            }

            var possibleRotations = HexRotation.All;
            var possiblePivots = HexRotation.All;
            var possiblePositions = new[] { UpperBonderPosition.Position, LowerBonderPosition.Position };
            var rotationCases = (
                from rotation in possibleRotations
                from pivot in possiblePivots
                from position in possiblePositions
                select (rotation, pivot, position)).ToList();

            var outputLocations = new Dictionary<int, OutputLocation>();

            // Recursively finds an output location for the specified builder and all subsequent builders.
            bool FindOutputLocations(int builderIndex)
            {
                var builder = m_builders[builderIndex];
                foreach (var (rot, pivot, pos) in rotationCases)
                {
                    var outputTransform = CalculateOutputTransform(builder, rot, pivot, pos);

                    // Check that none of the output atoms overlap any other atoms
                    var atomPositions = builder.Product.GetTransformedAtomPositions(GetWorldTransform().Apply(outputTransform)).ToList();
                    if (atomPositions.Any(p => GridState.CellContainsAnyObject(p)))
                    {
                        continue;
                    }

                    // Check that this output doesn't overlap another one
                    if (outputLocations.Values.Any(pos => atomPositions.Intersect(pos.AtomPositions).Any()))
                    {
                        continue;
                    }

                    outputLocations[builder.Product.ID] = new OutputLocation(outputTransform, atomPositions);

                    if (builderIndex == m_builders.Count - 1)
                    {
                        // This was the last output and we found the output location successfully
                        return true;
                    }
                    else if (FindOutputLocations(builderIndex + 1))
                    {
                        // All remaining output locations were found successfully
                        return true;
                    }
                    else
                    {
                        // One of the other outputs locations was found, so reset and try the another location for the current one
                        outputLocations.Remove(builder.Product.ID);
                    }
                }

                return false;
            }

            if (!FindOutputLocations(0))
            {
                throw new SolverException($"Could not find valid output locations for all products.");
            }

            foreach (var builder in m_builders)
            {
                var outputTransform = outputLocations[builder.Product.ID].Transform;
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
            if (builder.Product.Atoms.Count() == 1)
            {
                // For single-atom products we can just drop it straight on the output, without needing to move it onto the bonder first.
                ArmController.DropMoleculeAt(m_outputs[builder.Product.ID].Transform, this, addToGrid: false);
                yield break;
            }

            // Process the first atom
            var operations = builder.Operations;
            var op = operations[0];
            var assembledMolecule = ArmController.DropMoleculeAt(UpperBonderPosition, this);

            // Adjust the atom position so it matches the corresponding target molecule, but also update
            // the transform so that the atom has the same world transform as before.
            // TODO: Can we simplify this calculation?
            assembledMolecule.WorldTransform.Position = assembledMolecule.WorldTransform.Apply(assembledMolecule.Atoms[0].Position) - op.Atom.Position.RotateBy(op.MoleculeRotation);
            assembledMolecule.WorldTransform.Rotation = op.MoleculeRotation;
            assembledMolecule.Atoms[0].Position = op.Atom.Position;
            assembledMolecule.TargetMolecule = new AtomCollection(builder.Product, new Transform2D());
            yield return null;

            var targetBondPosition = LowerBonderPosition;

            for (int opIndex = 1; opIndex < operations.Count; opIndex++)
            {
                // Try to move the new atom onto the bonder
                if (!ArmController.MoveMoleculeTo(targetBondPosition, this, options: new ArmMovementOptions { AllowExternalBonds = true }, throwOnFailure: false))
                {
                    var newAtom = ArmController.DropMolecule();

                    var originalAssembledMoleculeTransform = assembledMolecule.WorldTransform;

                    // Move the partially assembled molecule out of the way
                    var moleculeTransform = GetWorldTransform().Inverse().Apply(originalAssembledMoleculeTransform);
                    if (targetBondPosition == UpperBonderPosition)
                    {
                        // Move it to the upper bonder position if necessary
                        moleculeTransform.Position += UpperBonderPosition.Position - LowerBonderPosition.Position;
                    }

                    // Rotate 120 degrees CCW
                    moleculeTransform = moleculeTransform.RotateAbout(ArmController.GrabberTransformToArmTransform(UpperBonderPosition).Position, HexRotation.R120);
                    
                    // Pivot 60 degrees CCW
                    moleculeTransform = moleculeTransform.RotateAbout(ArmController.GetRotatedGrabberTransform(UpperBonderPosition, HexRotation.R120).Position, HexRotation.R60);

                    ArmController.SetMoleculeToGrab(assembledMolecule);
                    ArmController.DropMoleculeAt(moleculeTransform, this);

                    // Stash the new atom "behind" the partially assembled molecule
                    ArmController.SetMoleculeToGrab(newAtom);
                    ArmController.DropMoleculeAt(ArmController.GetRotatedGrabberTransform(LowerBonderPosition, HexRotation.R120), this);

                    // Move the partially assembled molecule back onto the bonder
                    ArmController.SetMoleculeToGrab(assembledMolecule);
                    ArmController.DropMoleculeAt(originalAssembledMoleculeTransform);

                    // Grab the stashed atom again
                    ArmController.SetMoleculeToGrab(newAtom);
                    if (!ArmController.MoveMoleculeTo(targetBondPosition, this, options: new ArmMovementOptions { AllowExternalBonds = true }, throwOnFailure: false))
                    {
                        throw new SolverException($"Couldn't move atom to {targetBondPosition} even after repositioning the assembled molecule.");
                    }
                }

                ArmController.BondMoleculeToAtoms(assembledMolecule, m_bonder);

                if (opIndex == operations.Count - 1)
                {
                    FinishAssembly(builder, assembledMolecule);
                    yield return null;
                    yield break;
                }

                var nextOp = operations[opIndex + 1];

                // Figure out where we need to position the molecule for the next bond
                var targetTransform = new Transform2D(UpperBonderPosition.Position - nextOp.ParentAtom.Position, HexRotation.R0);
                targetTransform = targetTransform.RotateAbout(UpperBonderPosition.Position, nextOp.MoleculeRotation);

                // Make sure the molecule won't overlap the track when it's positioned at the target transform. However,
                // we will allow it to overlap the track cell that provides access to the upper bonder position as we don't
                // need to access that right now.
                bool moved = false;
                var atomPositions = assembledMolecule.GetTransformedAtomPositions(targetTransform, this);
                var upperTrackWorldPosition = ArmController.GrabberTransformToArmTransform(GetWorldTransform().Apply(UpperBonderPosition)).Position;
                if (!atomPositions.Any(p => p.position != upperTrackWorldPosition && GridState.GetTrack(p.position) != null))
                {
                    if (ArmController.MoveMoleculeTo(targetTransform, this, throwOnFailure: false))
                    {
                        targetBondPosition = LowerBonderPosition;
                        moved = true;
                    }
                }

                if (!moved)
                {
                    // Position it so we can bond on the other side of the bonder instead
                    targetTransform = new Transform2D(LowerBonderPosition.Position - nextOp.ParentAtom.Position, HexRotation.R0);
                    targetTransform = targetTransform.RotateAbout(LowerBonderPosition.Position, nextOp.MoleculeRotation + HexRotation.R180);
                    ArmController.MoveMoleculeTo(targetTransform, this);
                    targetBondPosition = UpperBonderPosition;
                }

                ArmController.DropMolecule();
                yield return null;
            }
        }

        private void FinishAssembly(MoleculeBuilder builder, AtomCollection assembledMolecule)
        {
            // Add any missing internal bonds
            foreach (var atom in assembledMolecule.Atoms)
            {
                foreach (var bondDir in HexRotation.All)
                {
                    var bondType = atom.Bonds[bondDir];
                    if (bondType == BondType.None && assembledMolecule.TargetMolecule.GetAtom(atom.Position).Bonds[bondDir] != BondType.None)
                    {                       
                        var rot = -bondDir + BondingDirection;
                        var targetTransform = new Transform2D(UpperBonderPosition.Position - atom.Position, HexRotation.R0);
                        targetTransform = targetTransform.RotateAbout(UpperBonderPosition.Position, rot);
                        if (!ArmController.MoveMoleculeTo(targetTransform, this, throwOnFailure: false))
                        {
                            targetTransform = new Transform2D(LowerBonderPosition.Position - atom.Position, HexRotation.R0);
                            targetTransform = targetTransform.RotateAbout(LowerBonderPosition.Position, rot + HexRotation.R180);
                            ArmController.MoveMoleculeTo(targetTransform, this);
                        }

                        var otherAtom = assembledMolecule.GetAtom(atom.Position + new Vector2(1, 0).RotateBy(bondDir));
                        assembledMolecule.AddBond(atom.Position, otherAtom.Position);
                    }
                }
            }

            // Move the molecule to the output
            ArmController.DropMoleculeAt(m_outputs[builder.Product.ID].Transform, this, addToGrid: false);
        }

        public static IEnumerable<MoleculeBuilder> CreateMoleculeBuilders(IEnumerable<Molecule> products)
        {
            return products.Select(p => new MoleculeBuilder(p)).ToList();
        }
    }
}
