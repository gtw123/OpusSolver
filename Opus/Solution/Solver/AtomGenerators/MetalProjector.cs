namespace Opus.Solution.Solver.AtomGenerators
{
    /// <summary>
    /// Generates an atom of a metal from an atom of a lower metal and quicksilver atoms.
    /// </summary>
    public class MetalProjector : AtomGenerator
    {
        private Arm m_projectionArm;
        private bool m_hasMetal;

        public MetalProjector(ProgramWriter writer)
            : base(writer)
        {
            new Glyph(this, new Vector2(0, 0), Direction.E, GlyphType.Projection);

            m_projectionArm = new Arm(this, new Vector2(0, 1), Direction.SW, MechanismType.Arm1);
            OutputArm = new Arm(this, new Vector2(4, 0), Direction.W, MechanismType.Arm1, extension: 3);
        }

        public override void Consume(Element element, int id)
        {
            if (!m_hasMetal)
            {
                Writer.WriteGrabResetAction(m_projectionArm, Instruction.RotateCounterclockwise);
                m_hasMetal = true;
            }
        }

        public override void Generate(Element element, int id)
        {
            // We can rotate the metal as soon as the last quicksilver is dropped, so grab it one cycle earlier
            Writer.AdjustTime(-1);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);

            m_hasMetal = false;
        }

        public override void PassThrough(Element element)
        {
            Writer.WriteGrabResetAction(m_projectionArm, Instruction.RotateCounterclockwise);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }
    }
}
