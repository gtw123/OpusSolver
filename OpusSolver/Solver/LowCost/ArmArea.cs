using System;
using System.Linq;
using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public class ArmArea : SolverComponent
    {
        private Arm m_mainArm { get; set; }
        private Track m_track { get; set; }

        private Transform2D m_armTransform;
        public Transform2D ArmTransform => m_armTransform;
        private ArmPathFinder m_armPathFinder;

        private AtomCollection m_grabbedAtoms;
        public AtomCollection GrabbedAtoms => m_grabbedAtoms;

        public GridState GridState { get; private set; } = new GridState();
        private RotationalCollisionDetector m_collisionDetector;

        public int ArmLength => 2;

        public ArmArea(SolverComponent parent, ProgramWriter writer)
            : base(parent, writer, new Vector2())
        {
            m_collisionDetector = new RotationalCollisionDetector(GridState);
        }

        public void CreateComponents(IEnumerable<Transform2D> requiredAccessPoints)
        {
            if (m_mainArm != null)
            {
                throw new InvalidOperationException("Cannot create components more than once.");
            }

            var armPoints = requiredAccessPoints.Select(t => GrabberTransformToArmPosition(t)).ToList();
            if (!armPoints.Any())
            {
                throw new InvalidOperationException("Expected at least one access point.");
            }

            CreateTrack(armPoints);
            CreateMainArm(GrabberTransformToArmTransform(requiredAccessPoints.First()));
            m_armPathFinder = new ArmPathFinder(ArmLength, m_track.GetAllPathCells(), GridState, m_collisionDetector);
        }

        private Transform2D GrabberTransformToArmTransform(Transform2D grabberTransform)
        {
            return grabberTransform.Apply(new Transform2D(new Vector2(-ArmLength, 0), HexRotation.R0));
        }

        private Vector2 GrabberTransformToArmPosition(Transform2D grabberTransform)
        {
            return grabberTransform.Apply(new Vector2(-ArmLength, 0));
        }

        private Vector2 GetGrabberPosition() => m_armTransform.Apply(new Vector2(ArmLength, 0));

        private void CreateTrack(IEnumerable<Vector2> armPoints)
        {
            Vector2 previousPoint = armPoints.First();
            var seenPoints = new HashSet<Vector2> { previousPoint };
            var segments = new List<Track.Segment>();
            foreach (var point in armPoints.Skip(1))
            {
                if (!seenPoints.Contains(point))
                {
                    var dir = (point - previousPoint).ToRotation() ?? throw new InvalidOperationException($"Cannot create a straight track segment from {previousPoint} to {point}.");
                    int length = point.DistanceBetween(previousPoint);
                    segments.Add(new Track.Segment(dir, length));

                    seenPoints.Add(point);
                    previousPoint = point;
                }
            }

            // Note that we may end up with 0 segments if there's only one arm points, but that's OK.
            // Having the track always created simplifies things, and the degenerate track will get
            // optimized away eventually anyway.
            m_track = new Track(this, armPoints.First(), segments);
        }

        private void CreateMainArm(Transform2D transform)
        {
            m_armTransform = transform;
            m_mainArm = new Arm(this, m_armTransform.Position, m_armTransform.Rotation, ArmType.Arm1, extension: ArmLength);
        }

        /// <summary>
        /// Moves the main arm so that its grabber will be at the specified position and the arm will rotated the
        /// specified direction (in local coordinates of a specifed object).
        /// </summary>
        /// <param name="grabberWorldTransform">The target position and rotation, in world coordinates</param>
        /// <param name="relativeToObj">The object whose local coordinate system the transform is specified in (if null, world coordinates are assumed)</param>
        /// <param name="armRotationOffset">Optional additional rotation to apply to the base of the arm</param>
        /// <param name="allowCalcification">Whether the arm is allowed to pass over a glyph of calcification if it'll change the grabbed atom</param>
        public void MoveGrabberTo(Transform2D grabberLocalTransform, GameObject relativeToObj = null, HexRotation? armRotationOffset = null, bool allowCalcification = false)
        {
            var grabberWorldTransform = relativeToObj?.GetWorldTransform().Apply(grabberLocalTransform) ?? grabberLocalTransform;

            var targetTransform = GrabberTransformToArmTransform(grabberWorldTransform);
            if (armRotationOffset != null)
            {
                targetTransform.Rotation += armRotationOffset.Value;
            }

            var instructions = m_armPathFinder.FindPath(m_armTransform, targetTransform, m_grabbedAtoms, allowCalcification);
            Writer.Write(m_mainArm, instructions);

            if (m_grabbedAtoms != null)
            {
                var relativeTransform = targetTransform.Apply(m_armTransform.Inverse());
                m_grabbedAtoms.WorldTransform = relativeTransform.Apply(m_grabbedAtoms.WorldTransform);
            }

            m_armTransform = targetTransform;
        }

        public void GrabAtoms(AtomCollection atoms, bool removeFromGrid = true)
        {
            if (m_grabbedAtoms != null)
            {
                throw new InvalidOperationException("Cannot grab atoms when already holding some.");
            }
            
            var grabberPosition = GetGrabberPosition();
            if (!atoms.GetWorldAtomPositions().Where(p => p.position == grabberPosition).Any())
            {
                throw new InvalidOperationException($"Cannot grab atoms as no atom is located at the current grabber position {grabberPosition}.");
            }

            if (removeFromGrid)
            {
                GridState.UnregisterAtoms(atoms);
            }

            m_grabbedAtoms = atoms;
            Writer.Write(m_mainArm, Instruction.Grab);
        }

        public AtomCollection DropAtoms(bool addToGrid = true)
        {
            if (m_grabbedAtoms == null)
            {
                throw new InvalidOperationException("Cannot drop atoms when not holding any.");
            }

            if (addToGrid)
            {
                GridState.RegisterAtoms(m_grabbedAtoms);
            }

            var atoms = m_grabbedAtoms;
            m_grabbedAtoms = null;

            Writer.Write(m_mainArm, Instruction.Drop);

            return atoms;
        }

        public void BondAtomsTo(AtomCollection bondToAtoms)
        {
            if (m_grabbedAtoms == null)
            {
                throw new InvalidOperationException("Cannot bond atoms when not holding any.");
            }

            GridState.UnregisterAtoms(bondToAtoms);
            foreach (var (atom, pos) in m_grabbedAtoms.GetWorldAtomPositions())
            {
                atom.Position = bondToAtoms.WorldTransform.Inverse().Apply(pos);
                bondToAtoms.AddAtom(atom);
            }

            m_grabbedAtoms = bondToAtoms;
        }

        public void PivotClockwise()
        {
            Writer.Write(m_mainArm, Instruction.PivotClockwise);
            if (m_grabbedAtoms != null)
            {
                m_grabbedAtoms.WorldTransform = m_grabbedAtoms.WorldTransform.RotateAbout(GetGrabberPosition(), -HexRotation.R60);
            }
        }

        public void PivotCounterClockwise()
        {
            Writer.Write(m_mainArm, Instruction.PivotCounterclockwise);
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
                throw new InvalidOperationException("Cannot pivot when not holding any atoms.");
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
                throw new InvalidOperationException("Cannot pivot when not holding any atoms.");
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
            Writer.Write(m_mainArm, Instruction.Reset);
            m_armTransform = m_mainArm.Transform;

            if (m_grabbedAtoms != null)
            {
                GridState.RegisterAtoms(m_grabbedAtoms);
                m_grabbedAtoms = null;
            }
        }
    }
}