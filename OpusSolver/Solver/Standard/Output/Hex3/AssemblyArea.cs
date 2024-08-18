namespace OpusSolver.Solver.Standard.Output.Hex3
{
    public class AssemblyArea : SolverComponent
    {
        public override Vector2 OutputPosition => new Vector2();

        public Arm HorizontalArm { get; private set; }
        public Arm AssemblyArm { get; private set; }

        private Glyph m_leftBonder;
        public bool IsLeftBonderUsed { get; set; } = false;

        public AssemblyArea(SolverComponent parent, ProgramWriter writer)
            : base(parent, writer, parent.OutputPosition)
        {
            new Glyph(this, new Vector2(0, 0), HexRotation.R0, GlyphType.Bonding);
            m_leftBonder = new Glyph(this, new Vector2(-2, 0), HexRotation.R0, GlyphType.Bonding);

            HorizontalArm = new Arm(this, new Vector2(3, 0), HexRotation.R180, ArmType.Arm1, extension: 3);
            new Track(this, new Vector2(4, 0), HexRotation.R180, 2);

            AssemblyArm = new Arm(this, new Vector2(1, -2), HexRotation.R60, ArmType.Arm1, extension: 2);
            new Track(this, new Vector2(1, -2), [
                new Track.Segment { Direction = HexRotation.R240, Length = 1 },
                    new Track.Segment { Direction = HexRotation.R300, Length = 1 }
            ]);
        }

        public void OptimizeParts()
        {
            if (!IsLeftBonderUsed)
            {
                m_leftBonder.Parent = null;
            }
        }
    }
}
