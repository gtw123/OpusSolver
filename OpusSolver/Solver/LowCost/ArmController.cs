using System.Linq;
using System.Reflection;

namespace OpusSolver.Solver.LowCost
{
    public class ArmController
    {
        private readonly Arm m_mainArm;
        private readonly ProgramWriter m_writer;
        private readonly GridState m_gridState;

        private readonly RotationalCollisionDetector m_collisionDetector;
        private readonly ArmPathFinder m_armPathFinder;

        private Transform2D m_armTransform;
        private AtomCollection m_grabbedMolecule;
        private AtomCollection m_moleculeToGrab;

        public Transform2D ArmTransform => m_armTransform;
        public AtomCollection GrabbedMolecule => m_grabbedMolecule;

        private int ArmLength => m_mainArm.Extension;

        public ArmController(Arm arm, Track track, GridState gridState, ProgramWriter writer)
        {
            m_mainArm = arm;
            m_writer = writer;
            m_gridState = gridState;

            m_collisionDetector = new RotationalCollisionDetector(gridState);
            m_armPathFinder = new ArmPathFinder(ArmLength, track, gridState, m_collisionDetector);

            m_armTransform = arm.Transform;
        }

        public Transform2D GrabberTransformToArmTransform(Transform2D grabberTransform)
        {
            return grabberTransform.Apply(new Transform2D(new Vector2(-ArmLength, 0), HexRotation.R0));
        }

        public Vector2 GetGrabberPosition() => m_armTransform.Apply(new Vector2(ArmLength, 0));

        public Transform2D GetRotatedGrabberTransform(Transform2D grabberTransform, HexRotation armRotationOffset)
        {
            return grabberTransform.RotateAbout(grabberTransform.Position - new Vector2(ArmLength, 0), armRotationOffset);
        }

        public void SetMoleculeToGrab(AtomCollection molecule)
        {
            if (m_grabbedMolecule != null)
            {
                throw new SolverException("Cannot set molecule to grab when already holding one.");
            }

            m_moleculeToGrab = molecule;
        }

        /// <summary>
        /// Moves the main arm so that its grabber will be at the specified position and the arm will rotated in the
        /// specified direction (in local coordinates of a specifed object).
        /// </summary>
        /// <param name="grabberLocalTransform">The target position and rotation</param>
        /// <param name="relativeToObj">The object whose local coordinate system the transform is specified in (if null, world coordinates are assumed)</param>
        /// <param name="armRotationOffset">Optional additional rotation to apply to the base of the arm</param>
        public bool MoveGrabberTo(Transform2D grabberLocalTransform, GameObject relativeToObj = null, HexRotation? armRotationOffset = null, ArmMovementOptions options = null, bool throwOnFailure = true)
        {
            var grabberWorldTransform = relativeToObj?.GetWorldTransform().Apply(grabberLocalTransform) ?? grabberLocalTransform;

            if (m_moleculeToGrab != null)
            {
                if (m_moleculeToGrab.Atoms.Count != 1)
                {
                    // If there's more than one atom then it's ambiguous where it's supposed to be moved to
                    throw new SolverException("Cannot call MoveGrabberTo with more than one atom to grab. Use MoveMoleculeTo instead.");
                }

                return MoveMoleculeTo(grabberWorldTransform, null, options: options, throwOnFailure);
            }

            var targetArmTransform = GrabberTransformToArmTransform(grabberWorldTransform);
            if (armRotationOffset != null)
            {
                targetArmTransform.Rotation += armRotationOffset.Value;
            }

            var result = m_armPathFinder.FindArmPath(m_armTransform, targetArmTransform, m_grabbedMolecule, options ?? new ArmMovementOptions());
            if (!result.Success)
            {
                return throwOnFailure ? throw new SolverException($"Cannot find path to move arm from {m_armTransform} to {targetArmTransform}.") : false;
            }

            m_writer.Write(m_mainArm, result.Instructions);

            m_armTransform = targetArmTransform;

            return true;
        }

        /// <summary>
        /// Moves the main arm so that its grabbed molecule will be at the specified transform.
        /// </summary>
        /// <param name="targetTarget">The target position and rotation of the molecule</param>
        /// <param name="relativeToObj">The object whose local coordinate system the transform is specified in (if null, world coordinates are assumed)</param>
        public bool MoveMoleculeTo(Transform2D targetTransform, GameObject relativeToObj = null, ArmMovementOptions options = null, bool throwOnFailure = true)
        {
            if (m_grabbedMolecule == null && m_moleculeToGrab == null)
            {
                throw new SolverException("Cannot move molecule when not holding one or SetMoleculeToGrab has not been called.");
            }

            targetTransform = relativeToObj?.GetWorldTransform().Apply(targetTransform) ?? targetTransform;

            var moleculeToMove = m_grabbedMolecule ?? m_moleculeToGrab;
            bool alreadyGrabbed = m_grabbedMolecule != null;
            if (!alreadyGrabbed)
            {
                m_gridState.UnregisterMolecule(moleculeToMove);
            }

            var result = m_armPathFinder.FindMoleculePath(m_armTransform, targetTransform, moleculeToMove, alreadyGrabbed, options ?? new ArmMovementOptions());
            if (!result.Success)
            {
                if (!alreadyGrabbed)
                {
                    m_gridState.RegisterMolecule(moleculeToMove);
                }
                return throwOnFailure ? throw new SolverException($"Cannot find path to move molecule from {moleculeToMove.WorldTransform} to {targetTransform}.") : false;
            }

            m_writer.Write(m_mainArm, result.Instructions);

            m_grabbedMolecule = moleculeToMove;
            m_armTransform = result.FinalArmTransform;
            m_moleculeToGrab = null;

            return true;
        }

        public void GrabMolecule(AtomCollection molecule)
        {
            if (m_grabbedMolecule != null)
            {
                throw new SolverException("Cannot grab a molecule when already holding one.");
            }
            
            var grabberPosition = GetGrabberPosition();
            if (!molecule.GetWorldAtomPositions().Where(p => p.position == grabberPosition).Any())
            {
                throw new SolverException($"Cannot grab a molecule as no atom is located at the current grabber position {grabberPosition}.");
            }

            m_gridState.UnregisterMolecule(molecule);

            m_grabbedMolecule = molecule;
            m_moleculeToGrab = null;
            m_writer.Write(m_mainArm, Instruction.Grab);
        }

        public AtomCollection DropMolecule(bool addToGrid = true)
        {
            if (m_moleculeToGrab != null)
            {
                var result = m_moleculeToGrab;
                m_moleculeToGrab = null;
                return result;
            }

            if (m_grabbedMolecule == null)
            {
                throw new SolverException("Cannot drop a molecule when not holding one.");
            }

            if (addToGrid)
            {
                m_gridState.RegisterMolecule(m_grabbedMolecule);
            }

            var molecule = m_grabbedMolecule;
            m_grabbedMolecule = null;

            m_writer.Write(m_mainArm, Instruction.Drop);

            return molecule;
        }

        /// <summary>
        /// Moves the grabbed molecule to the specified transform and then drops it.
        /// </summary>
        public AtomCollection DropMoleculeAt(Transform2D targetTransform, GameObject relativeToObj = null, bool addToGrid = true, ArmMovementOptions options = null)
        {
            MoveMoleculeTo(targetTransform, relativeToObj, options);
            return DropMolecule(addToGrid);
        }

        /// <summary>
        /// Bonds the grabbed molecule to another collection of atoms. The atoms from the grabbed molecule will be added
        /// to that collection.
        /// </summary>
        public void BondMoleculeToAtoms(AtomCollection bondToAtoms, Glyph bonder)
        {
            if (m_grabbedMolecule == null)
            {
                throw new SolverException("Cannot bond atoms when not holding a molecule.");
            }

            m_gridState.UnregisterMolecule(bondToAtoms);

            var bondToAtomsInverse = bondToAtoms.WorldTransform.Inverse();
            foreach (var (atom, pos) in m_grabbedMolecule.GetWorldAtomPositions())
            {
                atom.Position = bondToAtomsInverse.Apply(pos);
                bondToAtoms.AddAtom(atom);
            }

            void AddBond(Atom atom1, Atom atom2, BondType bondType)
            {
                if (atom1 != null && atom2 != null)
                {
                    bondToAtoms.AddBond(atom1.Position, atom2.Position, bondType);
                }
            }

            var bonderCells = bonder.GetWorldCells();
            var possibleAtoms = bonderCells.Select(p => bondToAtoms.GetAtom(bondToAtomsInverse.Apply(p))).ToArray();

            if (bonder.Type == GlyphType.Bonding)
            {
                AddBond(possibleAtoms[0], possibleAtoms[1], BondType.Single);
            }
            else if (bonder.Type == GlyphType.TriplexBonding)
            {
                AddBond(possibleAtoms[0], possibleAtoms[1], BondType.TriplexGray);
                AddBond(possibleAtoms[1], possibleAtoms[2], BondType.TriplexRed);
                AddBond(possibleAtoms[2], possibleAtoms[0], BondType.TriplexYellow);
            }
            else
            {
                throw new SolverException($"{nameof(BondMoleculeToAtoms)} doesn't currently support bonder type: {bonder.Type}.");
            }

            m_grabbedMolecule = bondToAtoms;
        }

        public AtomCollection UnbondGrabbedAtomFromOthers()
        {
            if (m_grabbedMolecule == null)
            {
                throw new SolverException("Cannot unbond atoms when not holding a molecule.");
            }

            var grabberPosition = GetGrabberPosition();
            var grabbedAtom = m_grabbedMolecule.GetAtomAtWorldPosition(grabberPosition);

            var droppedAtoms = m_grabbedMolecule;
            m_grabbedMolecule = m_grabbedMolecule.RemoveAtom(grabbedAtom);
            m_gridState.RegisterMolecule(droppedAtoms);

            return droppedAtoms;
        }

        public void PivotClockwise()
        {
            m_writer.Write(m_mainArm, Instruction.PivotClockwise);
            if (m_grabbedMolecule != null)
            {
                m_grabbedMolecule.WorldTransform = m_grabbedMolecule.WorldTransform.RotateAbout(GetGrabberPosition(), -HexRotation.R60);
            }
        }

        public void PivotCounterClockwise()
        {
            m_writer.Write(m_mainArm, Instruction.PivotCounterclockwise);
            if (m_grabbedMolecule != null)
            {
                m_grabbedMolecule.WorldTransform = m_grabbedMolecule.WorldTransform.RotateAbout(GetGrabberPosition(), HexRotation.R60);
            }
        }

        public void PivotBy(HexRotation deltaRot, bool rotateClockwiseIf180Degrees = false)
        {
            if (deltaRot == HexRotation.R0)
            {
                return;
            }

            if (m_grabbedMolecule == null)
            {
                throw new SolverException("Cannot pivot when not holding a molecule.");
            }

            foreach (var rot in HexRotation.R0.CalculateDeltaRotationsTo(deltaRot, rotateClockwiseIf180Degrees))
            {
                if (rot == HexRotation.R60)
                {
                    PivotCounterClockwise();
                }
                else
                {
                    PivotClockwise();
                }
            }
        }

        /// <summary>
        /// Attempts to pivot the currently held molecule by the specified amount, but only if this won't cause any collisions.
        /// </summary>
        /// <returns>True if the pivot was successful; false otherwise (in which case no instructions will be written)</returns>
        public bool TryPivotBy(HexRotation deltaRot, bool rotateClockwiseIf180Degrees = false)
        {
            if (deltaRot == HexRotation.R0)
            {
                return true;
            }

            if (m_grabbedMolecule == null)
            {
                throw new SolverException("Cannot pivot when not holding a molecule.");
            }

            var currentMoleculeTransform = m_grabbedMolecule.WorldTransform;
            var startRot = currentMoleculeTransform.Rotation;
            var grabberPosition = GetGrabberPosition();
            foreach (var pivot in startRot.CalculateDeltaRotationsTo(startRot + deltaRot, rotateClockwiseIf180Degrees))
            {
                if (m_collisionDetector.WillAtomsCollideWhilePivoting(m_grabbedMolecule, currentMoleculeTransform, m_armTransform.Position, grabberPosition, pivot))
                {
                    return false;
                }

                currentMoleculeTransform = currentMoleculeTransform.RotateAbout(grabberPosition, pivot);
            }

            PivotBy(deltaRot);

            return true;
        }

        public void ResetArm()
        {
            m_writer.Write(m_mainArm, Instruction.Reset);
            m_armTransform = m_mainArm.Transform;

            if (m_grabbedMolecule != null)
            {
                m_gridState.RegisterMolecule(m_grabbedMolecule);
                m_grabbedMolecule = null;
            }

            m_moleculeToGrab = null;
        }
    }
}