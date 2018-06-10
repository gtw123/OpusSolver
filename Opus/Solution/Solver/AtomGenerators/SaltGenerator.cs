namespace Opus.Solution.Solver.AtomGenerators
{
    /// <summary>
    /// Generates an atom of salt from an atom of a cardinal element.
    /// </summary>
    public class SaltGenerator : AtomGenerator
    {
        private Arm m_bigArm;
        private Arm m_smallArm;

        public SaltGenerator(ProgramWriter writer)
            : base(writer)
        {
            new Glyph(this, new Vector2(1, 0), Direction.E, GlyphType.Calcification);
            new Track(this, new Vector2(0, 2), Direction.E, 1);

            m_bigArm = new Arm(this, new Vector2(0, 2), Direction.SW, MechanismType.Arm1, extension: 2);
            m_smallArm = new Arm(this, new Vector2(1, 1), Direction.SW, MechanismType.Arm1);
            OutputArm = new Arm(this, new Vector2(5, 0), Direction.W, MechanismType.Arm1, extension: 3);
        }

        public override void Generate(Element element, int id)
        {
            Writer.WriteGrabResetAction(m_bigArm, Instruction.MovePositive);
            Writer.WriteGrabResetAction(m_smallArm, Instruction.RotateCounterclockwise);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }

        public override void PassThrough(Element element)
        {
            Writer.WriteGrabResetAction(m_bigArm, Instruction.RotateCounterclockwise);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }
    }
}
