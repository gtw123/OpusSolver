using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class VanBerloController
    {
        private readonly ProgramWriter m_writer;
        private readonly Arm m_wheelArm;

        private bool m_isFirstAtom = true;
        private HexRotation m_currentWheelRotation;

        // Elements that can be produced by Van Berlo's wheel when it has the specified rotation.
        public static HexRotationDictionary<Element> ProducedElements = new(new Dictionary<HexRotation, Element>
        { 
            { HexRotation.R0, Element.Salt },
            { HexRotation.R60, Element.Air},
            { HexRotation.R120, Element.Water },
            { HexRotation.R180, Element.Salt },
            { HexRotation.R240, Element.Earth },
            { HexRotation.R300, Element.Fire }
        });

        public VanBerloController(ProgramWriter writer, Arm wheelArm)
        {
            m_writer = writer;
            m_wheelArm = wheelArm;
        }

        public void RotateToElement(Element element)
        {
            var destRotation = ProducedElements.First(p => p.Value == element).Key;
            if (m_isFirstAtom)
            {
                // Set the initial rotation of the arm to the first element, to save a few instructions
                m_wheelArm.Transform.Rotation = destRotation;
                m_isFirstAtom = false;
            }
            else
            {
                var deltaRotations = m_currentWheelRotation.CalculateDeltaRotationsTo(destRotation);
                if (deltaRotations.Any())
                {
                    // Rotate the wheel before the atom gets into position
                    m_writer.AdjustTime(-deltaRotations.Count());
                    m_writer.Write(m_wheelArm, deltaRotations.ToRotationInstructions());
                }

                // Force the wheel to not rotate again until the atom is moving away. Otherwise salt
                // might get converted to something else and the reset may happen too early.
                m_writer.Write(m_wheelArm, Instruction.Wait, updateTime: false);
            }

            m_currentWheelRotation = destRotation;
        }

        public HexRotationDictionary<Element> GetCurrentElements()
        {
            return new HexRotationDictionary<Element>(
                HexRotation.All.ToDictionary(r => r, r => ProducedElements[m_currentWheelRotation - r]));
        }

        public void Reset(bool asEarlyAsPossible = true)
        {
            if (!asEarlyAsPossible)
            {
                m_writer.Write(m_wheelArm, Instruction.Reset);
                return;
            }

            // Add a reset instruction just after the last instruction written for the wheel arm.
            // Ideally we'd simply add a new fragment with a single instruction and let ProgramBuilder
            // automatically move it to the correct position but unfortunately it doesn't currently
            // support "floating" instructions like this. If it has adjacent fragments that share no arms
            // then it'll simply combine them together rather than adjusting them separately so that they
            // start as early as possible. This means we end up ranges of empty instructions in the
            // final program.
            var fragment = m_writer.GetLastFragmentForArm(m_wheelArm);
            if (fragment != null)
            {
                var instructions = fragment.GetArmInstructions(m_wheelArm);
                int lastIndex = instructions.FindLastIndex(i => i != Instruction.None);
                instructions.Insert(lastIndex + 1, Instruction.Reset);
            }
        }
    }
}
