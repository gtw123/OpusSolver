﻿using System.Collections.Generic;
using System.Linq;

namespace Opus.Solution.Solver.AtomGenerators
{
    /// <summary>
    /// Generates an atom of a cardinal element from salt using Van Berlo's wheel.
    /// </summary>
    public class VanBerloGenerator : AtomGenerator
    {    
        private Arm m_wheelArm;
        private bool m_isFirstAtom = true;
        private int m_currentWheelRotation;

        // Elements that can be produced by Van Berlo's wheel, in clockwise order
        private static List<Element> sm_wheelElements = new List<Element> { Element.Salt, Element.Air, Element.Water, Element.Salt, Element.Earth, Element.Fire };

        public VanBerloGenerator(ProgramWriter writer)
            : base(writer)
        {
            new Glyph(this, new Vector2(0, 1), Direction.SW, GlyphType.Duplication);
            m_wheelArm = new Arm(this, new Vector2(1, 1), Direction.E, MechanismType.VanBerlo);
            OutputArm = new Arm(this, new Vector2(3, 0), Direction.W, MechanismType.Arm1, extension: 3);
        }

        public override void Generate(Element element, int id)
        {
            GenerateAtomUsingWheel(element);
        }

        public override void PassThrough(Element element)
        {
            if (element == Element.Salt)
            {
                GenerateAtomUsingWheel(Element.Salt);
            }
            else
            {
                // Assume it's an atom that is unaffected by the glyph of duplication
                Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
            }
        }

        private void GenerateAtomUsingWheel(Element element)
        {
            int destRotation = sm_wheelElements.FindIndex(e => e == element);
            if (m_isFirstAtom)
            {
                // Set the initial rotation of the arm to the first element, to save a few instructions
                m_wheelArm.Rotation = destRotation;
                m_isFirstAtom = false;
            }
            else
            {
                int deltaRotation = (destRotation - m_currentWheelRotation + Direction.Count) % Direction.Count;
                if (deltaRotation > 0)
                {
                    int numRotations = deltaRotation;
                    Instruction instruction = Instruction.RotateCounterclockwise;
                    if (deltaRotation >= 3)
                    {
                        numRotations = Direction.Count - deltaRotation;
                        instruction = Instruction.RotateClockwise;
                    }

                    // Rotate the wheel before the atom gets into position
                    Writer.AdjustTime(-numRotations);
                    Writer.Write(m_wheelArm, Enumerable.Repeat(instruction, numRotations));
                }

                // Force the wheel to not rotate again until the atom is moving away. Otherwise salt
                // might get converted to something else and the reset may happen too early.
                Writer.Write(m_wheelArm, Instruction.Wait, updateTime: false);
            }

            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);

            m_currentWheelRotation = destRotation;
        }

        public override void EndSolution()
        {
            Writer.NewFragment();
            Writer.Write(m_wheelArm, Instruction.Reset);
        }
    }
}
