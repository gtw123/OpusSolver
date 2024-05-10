using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators
{
    /// <summary>
    /// Generates an atom of Mors and an atom of Vitae from two salt atoms.
    /// </summary>
    public class MorsVitaeGenerator : AtomGenerator
    {
        private Arm m_inputArm;
        private Arm m_saltArm;
        private Arm m_vitaeArm;
        private Arm m_morsArm;

        private LoopingCoroutine<object> m_consumeCoroutine;
        private LoopingCoroutine<Element> m_generateCoroutine;
        private Element m_generatingElement;

        public MorsVitaeGenerator(ProgramWriter writer)
            : base(writer)
        {
            m_consumeCoroutine = new LoopingCoroutine<object>(ConsumeSalt);
            m_generateCoroutine = new LoopingCoroutine<Element>(GenerateMorsVitae);

            m_inputArm = new Arm(this, new Vector2(2, -2), Direction.NW, MechanismType.Arm1, extension: 2);
            m_saltArm = new Arm(this, new Vector2(1, -1), Direction.NW, MechanismType.Arm1);
            new Glyph(this, new Vector2(1, 0), Direction.E, GlyphType.Animismus);

            m_vitaeArm = new Arm(this, new Vector2(2, 1), Direction.W, MechanismType.Arm1);
            m_morsArm = new Arm(this, new Vector2(3, -1), Direction.W, MechanismType.Arm1);
            OutputArm = new Arm(this, new Vector2(4, -2), Direction.NW, MechanismType.Arm1, extension: 2);
        }

        public override void Consume(Element element, int id)
        {
            m_consumeCoroutine.Next();
        }

        private IEnumerable<object> ConsumeSalt()
        {
            Writer.WriteGrabResetAction(m_inputArm, Instruction.RotateClockwise);
            yield return null;

            Writer.WriteGrabResetAction(m_saltArm, Instruction.RotateClockwise);
            yield return null;
        }

        public override void Generate(Element element, int id)
        {
            m_generatingElement = element;
            m_generateCoroutine.Next();
        }

        private IEnumerable<Element> GenerateMorsVitae()
        {
            Writer.AdjustTime(1);  // Wait for the atoms to be created
            MoveToOutput(m_generatingElement);
            yield return m_generatingElement;

            Writer.NewFragment();
            // Add some waits to the input arm to prevent collisions if another atom wants to
            // come through while we're still moving the last atom off the glyph.
            Writer.Write(m_inputArm, Enumerable.Repeat(Instruction.Wait, 2), updateTime: false);
            MoveToOutput(m_generatingElement);
            yield return m_generatingElement;
        }

        private void MoveToOutput(Element element)
        {
            if (element == Element.Mors)
            {
                Writer.WriteGrabResetAction(m_morsArm, Instruction.RotateClockwise);
            }
            else
            {
                Writer.WriteGrabResetAction(m_vitaeArm, Instruction.RotateCounterclockwise);
            }

            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateClockwise);
        }

        public override void PassThrough(Element element)
        {
            Writer.WriteGrabResetAction(m_inputArm, Instruction.RotateClockwise);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateClockwise);
        }
    }
}
