namespace OpusSolver.Solver.Standard
{
    /// <summary>
    /// Generates an atom of salt from an atom of a cardinal element.
    /// Optimized version for the case where no cardinal atoms need to be passed through without
    /// being calcified - i.e. every atom can move straight onto the calcification glyph.
    /// </summary>
    public class SaltGeneratorNoCardinalPassThrough : AtomGenerator
    {
        public SaltGeneratorNoCardinalPassThrough(ProgramWriter writer)
            : base(writer)
        {
            new Glyph(this, new Vector2(0, 0), HexRotation.R0, GlyphType.Calcification);
            OutputArm = new Arm(this, new Vector2(3, 0), HexRotation.R180, ArmType.Arm1, extension: 3);
        }

        public override void Generate(Element element, int id)
        {
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }

        public override void PassThrough(Element element)
        {
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }
    }
}
