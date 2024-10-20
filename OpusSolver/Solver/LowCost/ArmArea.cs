using System;
using System.Linq;
using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public class ArmArea : SolverComponent
    {
        private Arm m_mainArm;
        private Track m_track;

        public ArmController ArmController { get; private set; }

        public GridState GridState { get; private set; } = new GridState();

        public int ArmLength { get; private init; }

        public ArmArea(SolverComponent parent, ProgramWriter writer, int armLength)
            : base(parent, writer, new Vector2())
        {
            ArmLength = armLength;
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

            ArmController = new ArmController(m_mainArm, m_track, GridState, Writer);
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
            try
            {
                var path = new TrackPathBuilder(armPoints).CreateTrack();

                // Note that we may end up with 0 segments if there's only one arm point, but that's OK.
                // Having the track always created simplifies things, and the degenerate track will get
                // optimized away eventually anyway.
                m_track = new Track(this, path.StartPosition, path.Segments);
            }
            catch (Exception)
            {
                // Add some markers to help the user see why the track can't be created
                foreach (var point in armPoints)
                {
                    new Glyph(this, point, HexRotation.R0, GlyphType.Equilibrium);
                }

                throw;
            }
        }

        private void CreateMainArm(Transform2D transform)
        {
            m_mainArm = new Arm(this, transform.Position, transform.Rotation, ArmType.Arm1, extension: ArmLength);
        }
    }
}