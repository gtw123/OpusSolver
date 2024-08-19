﻿using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard
{
    /// <summary>
    /// Generates an atom of a cardinal element from salt using Van Berlo's wheel.
    /// </summary>
    public class VanBerloGenerator : AtomGenerator
    {    
        private Arm m_wheelArm;
        private bool m_isFirstAtom = true;
        private HexRotation m_currentWheelRotation;

        // Elements that can be produced by Van Berlo's wheel, in clockwise order
        private static List<Element> sm_wheelElements = new List<Element> { Element.Salt, Element.Air, Element.Water, Element.Salt, Element.Earth, Element.Fire };

        public VanBerloGenerator(ProgramWriter writer)
            : base(writer)
        {
            new Glyph(this, new Vector2(0, 1), HexRotation.R240, GlyphType.Duplication);
            m_wheelArm = new Arm(this, new Vector2(1, 1), HexRotation.R0, ArmType.VanBerlo);
            OutputArm = new Arm(this, new Vector2(3, 0), HexRotation.R180, ArmType.Arm1, extension: 3);
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
            var destRotation = new HexRotation(sm_wheelElements.FindIndex(e => e == element));
            if (m_isFirstAtom)
            {
                // Set the initial rotation of the arm to the first element, to save a few instructions
                m_wheelArm.Transform.Rotation = destRotation;
                m_isFirstAtom = false;
            }
            else
            {
                var deltaRotation = (destRotation - m_currentWheelRotation).IntValue;
                if (deltaRotation != 0)
                {
                    int numRotations = deltaRotation;
                    var instruction = Instruction.RotateCounterclockwise;
                    if (deltaRotation >= 3)
                    {
                        numRotations = HexRotation.Count - deltaRotation;
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
            // Add a reset instruction just after the last instruction written for this arm.
            // Ideally we'd simply add a new fragment with a single instruction and let ProgramBuilder
            // automatically move it to the correct position but unfortunately it doesn't currently
            // support "floating" instructions like this. If it has adjacent fragments that share no arms
            // then it'll simply combine them together rather than adjusting them separately so that they
            // start as early as possible. This means we end up ranges of empty instructions in the
            // final program.
            var fragment = Writer.GetLastFragmentForArm(m_wheelArm);
            if (fragment != null)
            {
                var instructions = fragment.GetArmInstructions(m_wheelArm);
                int lastIndex = instructions.FindLastIndex(i => i != Instruction.None);
                instructions.Insert(lastIndex + 1, Instruction.Reset);
            }
        }
    }
}