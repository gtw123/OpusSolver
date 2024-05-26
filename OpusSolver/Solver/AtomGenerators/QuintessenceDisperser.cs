using System.Collections.Generic;

namespace OpusSolver.Solver.AtomGenerators
{
    /// <summary>
    /// Generates atoms of the four cardinal elements from an atom of Quintessence.
    /// </summary>
    public class QuintessenceDisperser : AtomGenerator
    {
        private Arm m_inputArm;
        private Arm m_disperseArm;
        private Arm m_airFireArm;
        private Arm m_waterArm;

        private LoopingCoroutine<Element> m_generateCoroutine;

        public QuintessenceDisperser(ProgramWriter writer)
            : base(writer)
        {
            m_generateCoroutine = new LoopingCoroutine<Element>(GenerateCardinals);

            m_inputArm = new Arm(this, new Vector2(2, -2), HexRotation.R120, ArmType.Arm1, extension: 2);
            new Glyph(this, new Vector2(3, 0), HexRotation.R0, GlyphType.Dispersion);

            m_disperseArm = new Arm(this, new Vector2(2, 1), HexRotation.R240, ArmType.Arm1);
            m_airFireArm = new Arm(this, new Vector2(4, -2), HexRotation.R120, ArmType.Piston, extension: 2);
            m_waterArm = new Arm(this, new Vector2(4, 1), HexRotation.R240, ArmType.Piston, extension: 2);
            OutputArm = new Arm(this, new Vector2(6, -2), HexRotation.R120, ArmType.Arm1, extension: 2);
        }

        public override void Consume(Element element, int id)
        {
            Writer.WriteGrabResetAction(m_inputArm, Instruction.RotateClockwise);
            Writer.WriteGrabResetAction(m_disperseArm, Instruction.RotateCounterclockwise);
        }

        public override void Generate(Element element, int id)
        {
            m_generateCoroutine.Next();
        }

        private IEnumerable<Element> GenerateCardinals()
        {
            Writer.AdjustTime(1);  // Wait for the cardinal atoms to be created
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateClockwise);
            yield return Element.Earth;

            Writer.NewFragment();
            Writer.WriteGrabResetAction(m_airFireArm, Instruction.RotateClockwise);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateClockwise);
            yield return Element.Air;

            Writer.NewFragment();
            Writer.WriteGrabResetAction(m_waterArm, Instruction.Retract);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateClockwise);
            yield return Element.Water;

            Writer.NewFragment();
            Writer.Write(m_airFireArm, Instruction.Retract);
            Writer.WriteGrabResetAction(m_airFireArm, new[] { Instruction.RotateClockwise, Instruction.Extend });
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateClockwise);
            yield return Element.Fire;
        }

        public override void PassThrough(Element element)
        {
            Writer.WriteGrabResetAction(m_inputArm, Instruction.RotateClockwise);
            Writer.WriteGrabResetAction(m_airFireArm, Instruction.RotateClockwise);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateClockwise);
        }
    }
}
