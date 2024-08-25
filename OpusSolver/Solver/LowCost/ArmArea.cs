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

        public int ArmLength => 2;

        private int m_armTrackIndex;
        private Dictionary<Vector2, int> m_trackCellsToIndexes;

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
            m_trackCellsToIndexes = m_track.GetAllPathCells().Select((pos, index) => (pos, index)).ToDictionary(pair => pair.pos, pair => pair.index);
        }

        private void CreateMainArm(Transform2D transform)
        {
            m_armTrackIndex = 0;
            m_armTransform = transform;

            m_mainArm = new Arm(this, m_armTransform.Position, m_armTransform.Rotation, ArmType.Arm1, extension: ArmLength);
        }

        /// <summary>
        /// Moves the main arm so that its grabber will be at the specified position and the arm will rotated the
        /// specified direction (in local coordinates of a specifed object).
        /// </summary>
        /// <param name="obj">The object whose local coordinate system the transform is specified in</param>
        /// <param name="grabberWorldTransform">The target position and rotation, in world coordinates</param>
        /// <param name="armRotationOffset">Optional additional rotation to apply to the base of the arm</param>
        public void MoveGrabberTo(GameObject obj, Transform2D grabberlocalTransform, HexRotation? armRotationOffset = null)
        {
            MoveGrabberToWorldTransform(obj.GetWorldTransform().Apply(grabberlocalTransform), armRotationOffset);
        }

        /// <summary>
        /// Moves the main arm so that its grabber will be at the specified position and the arm will rotated the
        /// specified direction (in world coordinates).
        /// </summary>
        /// <param name="grabberWorldTransform">The target position and rotation, in world coordinates</param>
        /// <param name="armRotationOffset">Optional additional rotation to apply to the base of the arm</param>
        public void MoveGrabberToWorldTransform(Transform2D grabberWorldTransform, HexRotation? armRotationOffset = null)
        {
            var targetTransform = GrabberTransformToArmTransform(grabberWorldTransform);
            if (armRotationOffset != null)
            {
                targetTransform.Rotation += armRotationOffset.Value;
            }

            if (!m_trackCellsToIndexes.TryGetValue(targetTransform.Position, out var targetCellIndex))
            {
                throw new InvalidOperationException($"Can't move arm to {targetTransform.Position} because the track does not pass through that position.");
            }

            int cellDelta = targetCellIndex - m_armTrackIndex;
            var instruction = cellDelta > 0 ? Instruction.MovePositive : Instruction.MoveNegative;
            Writer.Write(m_mainArm, Enumerable.Repeat(instruction, Math.Abs(cellDelta)));
            m_armTransform.Position = targetTransform.Position;
            m_armTrackIndex = targetCellIndex;

            foreach (var rot in m_armTransform.Rotation.CalculateRotationsTo(targetTransform.Rotation))
            {
                instruction = ((rot - m_armTransform.Rotation) == HexRotation.R60) ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise;
                Writer.Write(m_mainArm, instruction);
                m_armTransform.Rotation = rot;
            }
        }

        public void GrabAtom()
        {
            Writer.Write(m_mainArm, Instruction.Grab);
        }

        public void DropAtom()
        {
            Writer.Write(m_mainArm, Instruction.Drop);
        }

        public void ResetArm()
        {
            Writer.Write(m_mainArm, Instruction.Reset);
            m_armTransform.Rotation = m_mainArm.Transform.Rotation;
        }
    }
}