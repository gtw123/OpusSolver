using System;

namespace OpusSolver.Solver.AtomGenerators
{
    /// <summary>
    /// Generates an atom of salt from an atom of a cardinal element.
    /// Optimized version for the case were passthrough isn't required.
    /// </summary>
    public class SaltGeneratorNoPassThrough : AtomGenerator
    {
        public SaltGeneratorNoPassThrough(ProgramWriter writer)
            : base(writer)
        {
            new Glyph(this, new Vector2(0, 0), Direction.E, GlyphType.Calcification);
            OutputArm = new Arm(this, new Vector2(3, 0), Direction.W, MechanismType.Arm1, extension: 3);
        }

        public override void Generate(Element element, int id)
        {
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }

        public override void PassThrough(Element element)
        {
            throw new InvalidOperationException("SaltGeneratorNoPassThrough doesn't allow pass through");
        }
    }
}
