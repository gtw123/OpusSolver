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
            yield return null;

            var targetBondPosition = LowerBonderPosition;

            for (int opIndex = 1; opIndex < operations.Count; opIndex++)
            {
                ArmController.MoveMoleculeTo(targetBondPosition, this, options: new ArmMovementOptions { AllowExternalBonds = true });
                ArmController.BondMoleculeToAtoms(assembledMolecule, m_bonder);

                if (opIndex == operations.Count - 1)
                {
                    ArmController.DropMoleculeAt(m_outputs[builder.Product.ID].Transform, this, addToGrid: false);
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

        public static bool IsProductCompatible(Molecule product)
        {
            //return product.Atoms.All(a => a.BondCount <= 2) && product.Atoms.Count(a => a.BondCount == 1) == 2 || product.Atoms.Count() == 1;
            // For now, only allow molecules with no bond loops
            return !DoesMoleculeHaveBondCycles(product);
        }

        private static bool DoesMoleculeHaveBondCycles(Molecule molecule)
        {
            var seenAtoms = new HashSet<Atom>();

            bool CheckForCycle(Atom currentAtom, Atom parent)
            {
                seenAtoms.Add(currentAtom);
                foreach (var (_, bondedAtom) in molecule.GetAdjacentBondedAtoms(currentAtom.Position))
                {
                    if (!seenAtoms.Contains(bondedAtom))
                    {
                        if (CheckForCycle(bondedAtom, currentAtom))
                        {
                            return true;
                        }
                    }
                    else if (bondedAtom != parent)
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var atom in molecule.Atoms)
            {
                if (!seenAtoms.Contains(atom) && CheckForCycle(atom, null))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<MoleculeBuilder> CreateMoleculeBuilders(IEnumerable<Molecule> products)
        {
            return products.Select(p => new MoleculeBuilder(p)).ToList();
        }
    }
}
