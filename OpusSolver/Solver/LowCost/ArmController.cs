using System.Linq;

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
        private AtomCollection m_grabbedAtoms;
        private AtomCollection m_atomsToGrab;

        public Transform2D ArmTransform => m_armTransform;
        public AtomCollection GrabbedAtoms => m_grabbedAtoms;

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

        private Transform2D GrabberTransformToArmTransform(Transform2D grabberTransform)
        {
            return grabberTransform.Apply(new Transform2D(new Vector2(-ArmLength, 0), HexRotation.R0));
        }

        public Vector2 GetGrabberPosition() => m_armTransform.Apply(new Vector2(ArmLength, 0));

        public Transform2D GetRotatedGrabberTransform(Transform2D grabberTransform, HexRotation armRotationOffset)
        {
            return grabberTransform.RotateAbout(grabberTransform.Position - new Vector2(ArmLength, 0), armRotationOffset);
        }

        /// <summary>
        /// Calculates what the world transform of the grabbed atoms would be if the grabber was moved from its
        /// current transform to the specified transform.
        /// </summary>
        public Transform2D GetAtomsTransformForGrabberTransform(Transform2D grabberLocalTransform, GameObject relativeToObj = null, HexRotation? armRotationOffset = null)
        {
            if (m_grabbedAtoms == null)
            {
                throw new SolverException("Cannot calculate atoms transform when not holding any atoms.");
            }

            var grabberWorldTransform = relativeToObj?.GetWorldTransform().Apply(grabberLocalTransform) ?? grabberLocalTransform;
            var targetArmTransform = GrabberTransformToArmTransform(grabberWorldTransform);
            return GetAtomsTransformForArmTransform(targetArmTransform, armRotationOffset: armRotationOffset);
        }

        /// <summary>
        /// Calculates what the world transform of the grabbed atoms would be if the arm was moved from its
        /// current transform to the specified transform.
        /// </summary>
        public Transform2D GetAtomsTransformForArmTransform(Transform2D armLocalTransform, GameObject relativeToObj = null, HexRotation? armRotationOffset = null)
        {
            if (m_grabbedAtoms == null)
            {
                throw new SolverException("Cannot calculate atoms transform when not holding any atoms.");
            }

            var targetArmTransform = relativeToObj?.GetWorldTransform().Apply(armLocalTransform) ?? armLocalTransform;
            if (armRotationOffset != null)
            {
                targetArmTransform.Rotation += armRotationOffset.Value;
            }

            var relativeTransform = targetArmTransform.Apply(m_armTransform.Inverse());
            return relativeTransform.Apply(m_grabbedAtoms.WorldTransform);
        }

        public void SetAtomsToGrab(AtomCollection atoms)
        {
            if (m_grabbedAtoms != null)
            {
                throw new SolverException("Cannot set atoms to grab when already holding some.");
            }

            m_atomsToGrab = atoms;
        }

        /// <summary>
        /// Moves the main arm so that its grabber will be at the specified position and the arm will rotated in the
        /// specified direction (in local coordinates of a specifed object).
        /// </summary>
        /// <param name="grabberLocalTransform">The target position and rotation</param>
        /// <param name="relativeToObj">The object whose local coordinate system the transform is specified in (if null, world coordinates are assumed)</param>
        /// <param name="armRotationOffset">Optional additional rotation to apply to the base of the arm</param>
        public void MoveGrabberTo(Transform2D grabberLocalTransform, GameObject relativeToObj = null, HexRotation? armRotationOffset = null, ArmMovementOptions options = null)
        {
            var grabberWorldTransform = relativeToObj?.GetWorldTransform().Apply(grabberLocalTransform) ?? grabberLocalTransform;

            if (m_atomsToGrab != null)
            {
                if (m_atomsToGrab.Atoms.Count != 1)
                {
                    // If there's more than one atom then it's ambiguous where it's supposed to be moved to
                    throw new SolverException("Cannot call MoveGrabberTo with more than one atom to grab. Use MoveAtomsTo instead.");
                }

                MoveAtomsTo(grabberWorldTransform, options: options);
                return;
            }

            var targetArmTransform = GrabberTransformToArmTransform(grabberWorldTransform);
            if (armRotationOffset != null)
            {
                targetArmTransform.Rotation += armRotationOffset.Value;
            }

            var instructions = m_armPathFinder.FindArmPath(m_armTransform, targetArmTransform, m_grabbedAtoms, options ?? new ArmMovementOptions());
            m_writer.Write(m_mainArm, instructions);

            if (m_grabbedAtoms != null)
            {
                m_grabbedAtoms.WorldTransform = GetAtomsTransformForArmTransform(targetArmTransform);
            }

            m_armTransform = targetArmTransform;
        }

        /// <summary>
        /// Moves the main arm so that its grabbed molecule will at the specified transform.
        /// </summary>
        /// <param name="targetTarget">The target position and rotation of the molecule</param>
        /// <param name="relativeToObj">The object whose local coordinate system the transform is specified in (if null, world coordinates are assumed)</param>
        public void MoveAtomsTo(Transform2D targetTransform, GameObject relativeToObj = null, ArmMovementOptions options = null)
        {
            if (m_grabbedAtoms == null && m_atomsToGrab == null)
            {
                throw new SolverException("Cannot move atoms when not holding any or SetAtomsToGrab has not been called.");
            }

            targetTransform = relativeToObj?.GetWorldTransform().Apply(targetTransform) ?? targetTransform;

            var atomsToMove = m_grabbedAtoms ?? m_atomsToGrab;
            bool alreadyGrabbed = m_grabbedAtoms != null;
            if (!alreadyGrabbed)
            {
                m_gridState.UnregisterAtoms(atomsToMove);
            }

            var (instructions, finalArmTransform) = m_armPathFinder.FindMoleculePath(m_armTransform, targetTransform, atomsToMove, alreadyGrabbed, options ?? new ArmMovementOptions());
            m_writer.Write(m_mainArm, instructions);

            m_grabbedAtoms = atomsToMove;
            m_grabbedAtoms.WorldTransform = targetTransform;
            m_armTransform = finalArmTransform;
            m_atomsToGrab = null;
        }

        public void GrabAtoms(AtomCollection atoms, bool removeFromGrid = true)
        {
            if (m_grabbedAtoms != null)
            {
                throw new SolverException("Cannot grab atoms when already holding some.");
            }
            
            var grabberPosition = GetGrabberPosition();
            if (!atoms.GetWorldAtomPositions().Where(p => p.position == grabberPosition).Any())
            {
                throw new SolverException($"Cannot grab atoms as no atom is located at the current grabber position {grabberPosition}.");
            }

            if (removeFromGrid)
            {
                m_gridState.UnregisterAtoms(atoms);
            }

            m_grabbedAtoms = atoms;
            m_atomsToGrab = null;
            m_writer.Write(m_mainArm, Instruction.Grab);
        }

        public AtomCollection DropAtoms(bool addToGrid = true)
        {
            if (m_grabbedAtoms == null)
            {
                throw new SolverException("Cannot drop atoms when not holding any.");
            }

            if (addToGrid)
            {
                m_gridState.RegisterAtoms(m_grabbedAtoms);
            }

            var atoms = m_grabbedAtoms;
            m_grabbedAtoms = null;

            m_writer.Write(m_mainArm, Instruction.Drop);

            return atoms;
        }

        /// <summary>
        /// Bonds the grabbed atoms to another collection of atoms. The grabbed atoms will be added
        /// to that collection.
        /// </summary>
        public void BondAtomsTo(AtomCollection bondToAtoms, Glyph bonder)
        {
            if (m_grabbedAtoms == null)
            {
                throw new SolverException("Cannot bond atoms when not holding any.");
            }

            if (bonder.Type != GlyphType.Bonding)
            {
                throw new SolverException($"{nameof(BondAtomsTo)} currently supports single bonders only.");
            }

            m_gridState.UnregisterAtoms(bondToAtoms);

            var bondToAtomsInverse = bondToAtoms.WorldTransform.Inverse();
            foreach (var (atom, pos) in m_grabbedAtoms.GetWorldAtomPositions())
            {
                atom.Position = bondToAtomsInverse.Apply(pos);
                bondToAtoms.AddAtom(atom);
            }

            var bonderCells = bonder.GetWorldCells();
            bondToAtoms.AddBond(bondToAtomsInverse.Apply(bonderCells[0]), bondToAtomsInverse.Apply(bonderCells[1]));

            m_grabbedAtoms = bondToAtoms;
        }

        public AtomCollection RemoveAllExceptGrabbedAtom()
        {
            if (m_grabbedAtoms == null)
            {
                throw new SolverException("Cannot unbond atoms when not holding any.");
            }

            var grabberPosition = GetGrabberPosition();
            var grabbedAtom = m_grabbedAtoms.GetAtomAtWorldPosition(grabberPosition);

            m_grabbedAtoms.RemoveAtom(grabbedAtom);
            var droppedAtoms = m_grabbedAtoms;
            m_gridState.RegisterAtoms(droppedAtoms);

            m_grabbedAtoms = new AtomCollection(grabbedAtom.Element, new Transform2D(grabberPosition, HexRotation.R0));

            return droppedAtoms;
        }

        public void PivotClockwise()
        {
            m_writer.Write(m_mainArm, Instruction.PivotClockwise);
            if (m_grabbedAtoms != null)
            {
                m_grabbedAtoms.WorldTransform = m_grabbedAtoms.WorldTransform.RotateAbout(GetGrabberPosition(), -HexRotation.R60);
            }
        }

        public void PivotCounterClockwise()
        {
            m_writer.Write(m_mainArm, Instruction.PivotCounterclockwise);
            if (m_grabbedAtoms != null)
            {
                m_grabbedAtoms.WorldTransform = m_grabbedAtoms.WorldTransform.RotateAbout(GetGrabberPosition(), HexRotation.R60);
            }
        }

        public void PivotBy(HexRotation deltaRot, bool rotateClockwiseIf180Degrees = false)
        {
            if (deltaRot == HexRotation.R0)
            {
                return;
            }

            if (m_grabbedAtoms == null)
            {
                throw new SolverException("Cannot pivot when not holding any atoms.");
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
        /// Attempts to pivot the currently held atoms by the specified amount, but only if this won't cause any collisions.
        /// </summary>
        /// <returns>True if the pivot was successful; false otherwise (in which case no instructions will be written)</returns>
        public bool TryPivotBy(HexRotation deltaRot, bool rotateClockwiseIf180Degrees = false)
        {
            if (deltaRot == HexRotation.R0)
            {
                return true;
            }

            if (m_grabbedAtoms == null)
            {
                throw new SolverException("Cannot pivot when not holding any atoms.");
            }

            var currentAtomsTransform = m_grabbedAtoms.WorldTransform;
            var startRot = currentAtomsTransform.Rotation;
            var grabberPosition = GetGrabberPosition();
            foreach (var pivot in startRot.CalculateDeltaRotationsTo(startRot + deltaRot, rotateClockwiseIf180Degrees))
            {
                if (m_collisionDetector.WillAtomsCollideWhilePivoting(m_grabbedAtoms, currentAtomsTransform, m_armTransform.Position, grabberPosition, pivot))
                {
                    return false;
                }

                currentAtomsTransform = currentAtomsTransform.RotateAbout(grabberPosition, pivot);
            }

            PivotBy(deltaRot);

            return true;
        }

        public void ResetArm()
        {
            m_writer.Write(m_mainArm, Instruction.Reset);
            m_armTransform = m_mainArm.Transform;

            if (m_grabbedAtoms != null)
            {
                m_gridState.RegisterAtoms(m_grabbedAtoms);
                m_grabbedAtoms = null;
            }

            m_atomsToGrab = null;
        }
    }
}