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

        private Element? m_grabbedElement;

        public GridState GridState { get; private set; } = new GridState();

        public int ArmLength => 2;

        public ArmArea(SolverComponent parent, ProgramWriter writer)
            : base(parent, writer, new Vector2())
        {
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
            m_armPathFinder = new ArmPathFinder(ArmLength, m_track.GetAllPathCells(), GridState);
        }

        private Transform2D GrabberTransformToArmTransform(Transform2D grabberTransform)
        {
            return grabberTransform.Apply(new Transform2D(new Vector2(-ArmLength, 0), HexRotation.R0));
        }

        private Vector2 GrabberTransformToArmPosition(Transform2D grabberTransform)
        {
            return grabberTransform.Apply(new Vector2(-ArmLength, 0));
        }

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
        public void MoveGrabberTo(Transform2D grabberlocalTransform, GameObject relativeToObj = null, HexRotation? armRotationOffset = null, bool allowCalcification = false)
        {
            var grabberWorldTransform = relativeToObj?.GetWorldTransform().Apply(grabberlocalTransform) ?? grabberlocalTransform;

            var targetTransform = GrabberTransformToArmTransform(grabberWorldTransform);
            if (armRotationOffset != null)
            {
                targetTransform.Rotation += armRotationOffset.Value;
            }

            var disallowedGlyphs = new HashSet<GlyphType>();
            if (m_grabbedElement.HasValue && !allowCalcification && PeriodicTable.Cardinals.Contains(m_grabbedElement.Value))
            {
                disallowedGlyphs.Add(GlyphType.Calcification);
            }

            var instructions = m_armPathFinder.FindPath(m_armTransform, targetTransform, m_grabbedElement, disallowedGlyphs);
            Writer.Write(m_mainArm, instructions);

            m_armTransform = targetTransform;
        }

        public void GrabAtom(Element element)
        {
            m_grabbedElement = element;
            Writer.Write(m_mainArm, Instruction.Grab);
        }

        public void DropAtom()
        {
            Writer.Write(m_mainArm, Instruction.Drop);
            m_grabbedElement = null;
        }

        public void SetGrabbedElement(Element element)
        {
            m_grabbedElement = element;
        }

        public void PivotClockwise()
        {
            Writer.Write(m_mainArm, Instruction.PivotClockwise);
        }

        public void PivotCounterClockwise()
        {
            Writer.Write(m_mainArm, Instruction.PivotCounterclockwise);
        }

        public void Pivot(HexRotation deltaRot, bool rotateClockwiseIf180Degrees = false)
        {
            if (deltaRot.IntValue < 3 || (deltaRot == HexRotation.R180 && !rotateClockwiseIf180Degrees))
            {
                for (int i = 0; i < deltaRot.IntValue; i++)
                {
                    PivotCounterClockwise();
                }
            }
            else
            {
                for (int i = 0; i < HexRotation.Count - deltaRot.IntValue; i++)
                {
                    PivotClockwise();
                }
            }
        }

        public void ResetArm()
        {
            Writer.Write(m_mainArm, Instruction.Reset);
            m_armTransform = m_mainArm.Transform;
            m_grabbedElement = null;
        }
    }
}