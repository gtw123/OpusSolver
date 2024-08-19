namespace OpusSolver.Solver.Standard
{
    /// <summary>
    /// Generates an atom of a cardinal element from salt using Van Berlo's wheel.
    /// </summary>
    public class VanBerloGenerator : AtomGenerator
    {    
        private readonly Arm m_wheelArm;
        private readonly VanBerloController m_controller;

        public VanBerloGenerator(ProgramWriter writer)
            : base(writer)
        {
            new Glyph(this, new Vector2(0, 1), HexRotation.R240, GlyphType.Duplication);
            m_wheelArm = new Arm(this, new Vector2(1, 1), HexRotation.R0, ArmType.VanBerlo);
            OutputArm = new Arm(this, new Vector2(3, 0), HexRotation.R180, ArmType.Arm1, extension: 3);

            m_controller = new VanBerloController(Writer, m_wheelArm);
        }

        public override void Generate(Element element, int id)
        {
            m_controller.RotateToElement(element);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }

        public override void PassThrough(Element element)
        {
            if (element == Element.Salt)
            {
                m_controller.RotateToElement(element);
            }
            else
            {
                // Assume it's an atom that is unaffected by the glyph of duplication
            }

            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }

        public override void EndSolution()
        {
            m_controller.Reset();
        }
    }
}
