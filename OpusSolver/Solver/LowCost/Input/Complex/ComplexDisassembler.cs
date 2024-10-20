using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Input.Complex
{
    public class ComplexDisassembler : LowCostAtomGenerator
    {
        private IEnumerable<MoleculeDismantler> m_dismantlers;
        private readonly Dictionary<int, LoopingCoroutine<object>> m_disassembleCoroutines;
        private readonly Glyph m_unbonder;

        private MoleculeInput m_input;

        public const int MaxReagents = 1;

        public override int RequiredWidth => 2;

        private static readonly Transform2D LowerUnbonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D UpperUnbonderPosition = new Transform2D(new Vector2(-1, 1), HexRotation.R0);
        private static readonly Transform2D ExtraAccessPoint = new Transform2D(new Vector2(-1, 0), HexRotation.R0);

        private List<Transform2D> m_accessPoints = [];
        public override IEnumerable<Transform2D> RequiredAccessPoints => m_accessPoints;

        /// <summary>
        /// The direction in which this disassembler's unbonder removes bonds, from the previous atom to the new atom.
        /// </summary>
        public static HexRotation UnbondingDirection = HexRotation.R300;

        public ComplexDisassembler(ProgramWriter writer, ArmArea armArea, IEnumerable<MoleculeDismantler> dismantlers, bool addExtraAccessPoint)
            : base(writer, armArea)
        {
            m_dismantlers = dismantlers;
            m_disassembleCoroutines = dismantlers.ToDictionary(d => d.Molecule.ID, d => new LoopingCoroutine<object>(() => Disassemble(d)));
            m_unbonder = new Glyph(this, LowerUnbonderPosition.Position, UnbondingDirection - HexRotation.R180, GlyphType.Unbonding);

            if (m_dismantlers.Count() > MaxReagents)
            {
                throw new SolverException($"{nameof(ComplexDisassembler)} currently only supports {MaxReagents} reagents (requested {m_dismantlers.Count()}).");
            }

            m_accessPoints.Add(LowerUnbonderPosition);
            m_accessPoints.Add(UpperUnbonderPosition);
            if (addExtraAccessPoint)
            {
                m_accessPoints.Add(ExtraAccessPoint);
            }

            AddInput(m_dismantlers.First().Molecule);
        }

        private void AddInput(Molecule molecule)
        {
            IEnumerable<Atom> FindAtomsToGrab()
            {
                for (int x = 0; x < molecule.Width; x++)
                {
                    for (int y = 0; y <= x; y++)
                    {
                        var atomToGrab = molecule.GetAtom(new(x - y, y));
                        if (atomToGrab != null)
                        {
                            yield return atomToGrab;
                        }
                    }
                }
            }

            var unbonderCells = m_unbonder.GetCells();
            foreach (var atom in FindAtomsToGrab())
            {
                var moleculeTransform = new Transform2D(LowerUnbonderPosition.Position - atom.Position, HexRotation.R0);
                var armPos = LowerUnbonderPosition.Position - new Vector2(ArmArea.ArmLength, 0);
                var inputTransform = moleculeTransform.RotateAbout(armPos, -HexRotation.R60);

                // Make sure the molecule won't overlap the unbonder
                var atomPositions = molecule.GetTransformedAtomPositions(GetWorldTransform().Apply(inputTransform));
                if (atomPositions.Intersect(unbonderCells).Any())
                {
                    continue;
                }

                // Make sure there's a gap between the molecule and the unbonder, otherwise it may be too difficult to unbond atoms
                if (atomPositions.Any(p => p.Y >= LowerUnbonderPosition.Position.Y - 1))
                {
                    continue;
                }

                m_input = new MoleculeInput(this, Writer, ArmArea, inputTransform, molecule, new Transform2D());
                return;
            }

            throw new SolverException("Couldn't find a valid input location for the reagent.");
        }

        public override void Generate(Element element, int id)
        {
            m_disassembleCoroutines[id].Next();
        }

        private IEnumerable<object> Disassemble(MoleculeDismantler dismantler)
        {
            AtomCollection remainingAtoms = null;

            for (int opIndex = 0; opIndex < dismantler.Operations.Count - 1; opIndex++)
            {
                if (opIndex == 0)
                {
                    remainingAtoms = m_input.GrabMolecule();
                    remainingAtoms.TargetMolecule = dismantler.BondReducedMolecule.Copy();
                }
                else
                {
                    ArmController.SetMoleculeToGrab(remainingAtoms);
                }

                var op = dismantler.Operations[opIndex];

                Vector2 targetUnbondPosition = new();
                var bondedAtoms = remainingAtoms.GetAdjacentBondedAtoms(remainingAtoms.GetAtom(op.Atom.Position)).ToList();
                if (bondedAtoms.Count > 1)
                {
                    // Make sure the bond referenced by the operation comes last in the list
                    bondedAtoms = bondedAtoms.Where(b => b.Value.Position != op.NextAtom.Position).Append(bondedAtoms.Single(b => b.Value.Position == op.NextAtom.Position)).ToList();
                }

                var options = new ArmMovementOptions
                {
                    AllowUnbonding = true,
                    FinalBondToRemove = (op.Atom.Position, op.NextAtom.Position)
                };

                foreach (var (bondDir, otherAtom) in bondedAtoms)
                {
                    // Skip if the bond has already been removed as a side-effect of removing the other bonds
                    if (remainingAtoms.GetAtom(op.Atom.Position).Bonds[bondDir] == BondType.None)
                    {
                        continue;
                    }

                    var requiredRotation = -bondDir + UnbondingDirection;

                    targetUnbondPosition = UpperUnbonderPosition.Position;
                    var targetTransform = new Transform2D(targetUnbondPosition - op.Atom.Position, HexRotation.R0);
                    targetTransform = targetTransform.RotateAbout(targetUnbondPosition, requiredRotation);

                    // Make sure the molecule won't overlap the track when it's positioned at the target transform. However,
                    // we will allow it to overlap the track cell that provides access to the lower unbonder position as we don't
                    // need to access that right now.
                    bool moved = false;
                    var atomPositions = remainingAtoms.GetTransformedAtomPositions(targetTransform, this);
                    var lowerTrackWorldPosition = ArmController.GrabberTransformToArmTransform(GetWorldTransform().Apply(LowerUnbonderPosition)).Position;
                    if (!atomPositions.Any(p => p.position != lowerTrackWorldPosition && GridState.GetTrack(p.position) != null))
                    {
                        if (ArmController.MoveMoleculeTo(targetTransform, this, options: options, throwOnFailure: false))
                        {
                            moved = true;
                        }
                    }

                    if (!moved)
                    {
                        targetUnbondPosition = LowerUnbonderPosition.Position;
                        targetTransform = new Transform2D(targetUnbondPosition - op.Atom.Position, HexRotation.R0);
                        targetTransform = targetTransform.RotateAbout(targetUnbondPosition, requiredRotation + HexRotation.R180);
                        ArmController.MoveMoleculeTo(targetTransform, this, options: options);
                    }

                    remainingAtoms.RemoveBond(op.Atom.Position, otherAtom.Position);
                }

                // Make sure we're grabbing the atom we want to move
                if (ArmController.GetGrabberPosition() != GetWorldTransform().Apply(targetUnbondPosition))
                {
                    var molecule = ArmController.DropMolecule();
                    var removedAtom = molecule.RemoveAtom(molecule.GetAtomAtWorldPosition(targetUnbondPosition, this));
                    ArmController.SetMoleculeToGrab(removedAtom);

                    remainingAtoms = molecule;
                }
                else
                {
                    remainingAtoms = ArmController.UnbondGrabbedAtomFromOthers();
                }

                remainingAtoms.TargetMolecule.RemoveAtom(remainingAtoms.TargetMolecule.GetAtom(op.Atom.Position));

                var newAtomPositions = remainingAtoms.GetTransformedAtomPositions(GetWorldTransform().Inverse().Apply(remainingAtoms.WorldTransform));
                if (newAtomPositions.Any(p => p.position == UpperUnbonderPosition.Position + new Vector2(-1, 1)))
                {
                    if (targetUnbondPosition == UpperUnbonderPosition.Position)
                    {
                        // Move the molecule out the way of the unbonded atom
                        var targetTransform = remainingAtoms.WorldTransform.RotateAbout(GetWorldTransform().Apply(LowerUnbonderPosition).Position, -HexRotation.R60);
                        var droppedMolecule = ArmController.DropMolecule();
                        ArmController.SetMoleculeToGrab(remainingAtoms);
                        ArmController.DropMoleculeAt(targetTransform);
                        ArmController.SetMoleculeToGrab(droppedMolecule);
                    }
                }

                yield return null;
            }

            // Recreate the atom collection so we can be sure it's at (0, 0) and with no bonds
            remainingAtoms = new AtomCollection(remainingAtoms.Atoms[0].Element, GetWorldTransform().Apply(LowerUnbonderPosition));
            ArmController.SetMoleculeToGrab(remainingAtoms);
            yield return null;
        }

        public static IEnumerable<MoleculeDismantler> CreateMoleculeDismantlers(IEnumerable<Molecule> reagents, bool reverseElementOrder, bool reverseBondTraversalDirection)
        {
            return reagents.Select(p => new MoleculeDismantler(p, reverseElementOrder, reverseBondTraversalDirection)).ToList();
        }
    }
}
