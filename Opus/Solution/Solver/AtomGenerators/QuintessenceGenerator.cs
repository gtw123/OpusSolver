using System.Collections.Generic;

namespace Opus.Solution.Solver.AtomGenerators
{
    /// <summary>
    /// Generates an atom of Quintessence from atoms of the four cardinal elements.
    /// </summary>
    public class QuintessenceGenerator : AtomGenerator
    {
        private Arm m_leftArm;
        private Arm m_rightArm;

        private LoopingCoroutine<object> m_consumeCoroutine;

        public QuintessenceGenerator(ProgramWriter writer)
            : base(writer)
        {
            m_consumeCoroutine = new LoopingCoroutine<object>(ConsumeCardinals);

            new Glyph(this, new Vector2(1, -1), Direction.E, GlyphType.Unification);

            m_leftArm = new Arm(this, new Vector2(0, -1), Direction.NE, MechanismType.Arm1);
            m_rightArm = new Arm(this, new Vector2(2, 0), Direction.W, MechanismType.Piston, extension: 2);
            OutputArm = new Arm(this, new Vector2(4, -1), Direction.W, MechanismType.Arm1, extension: 3);
        }

        public override void Consume(Element element, int id)
        {
            m_consumeCoroutine.Next();
        }

        private IEnumerable<object> ConsumeCardinals()
        {
            Writer.WriteGrabResetAction(m_rightArm, Instruction.RotateCounterclockwise);
            yield return null;

            Writer.WriteGrabResetAction(m_leftArm, new[] { Instruction.RotateClockwise, Instruction.RotateClockwise });
            yield return null;

            Writer.WriteGrabResetAction(m_rightArm, Instruction.Retract);
            yield return null;

            // Nothing to do for the fourth atom
            yield return null;
        }

        public override void Generate(Element element, int id)
        {
            Writer.AdjustTime(1);  // Wait for the quintessence atom to be generated
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }

        public override void PassThrough(Element element)
        {
            Writer.WriteGrabResetAction(m_leftArm, Instruction.RotateClockwise);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }
    }
}
