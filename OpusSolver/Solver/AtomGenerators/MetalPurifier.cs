using System;
using System.Collections.Generic;

namespace OpusSolver.Solver.AtomGenerators
{
    /// <summary>
    /// Generates an atom of a metal from atoms of lower metals.
    /// </summary>
    public class MetalPurifier : AtomGenerator
    {
        private class Purifier
        {
            public Arm BigArm;
            public Arm SmallArm;
        }

        private List<Purifier> m_purifiers = new List<Purifier>();

        private int m_size;
        private LoopingCoroutine<object> m_consumeCoroutine;

        private Element m_targetMetal;
        private int m_numPurifiers;

        public MetalPurifier(ProgramWriter writer, int size)
            : base(writer)
        {
            m_size = size;
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException("size", size, "Size must be 1 or greater.");
            }

            m_consumeCoroutine = new LoopingCoroutine<object>(ConsumeMetal);

            CreateObjects();
        }

        private void CreateObjects()
        {
            for (int i = 0; i < m_size; i++)
            {
                var purifier = new Purifier();
                var pos = new Vector2(i * 2, 1);

                new Glyph(this, pos, HexRotation.R240, GlyphType.Purification);

                // Use a piston for the first arm as we also use it to move atoms between the cells of the glyph
                var armType = (i == 0) ? MechanismType.Piston : MechanismType.Arm1;
                purifier.BigArm = new Arm(this, pos.Add(0, 1), HexRotation.R240, armType, extension: 2);
                purifier.SmallArm = new Arm(this, pos.Add(1, 0), HexRotation.R240, MechanismType.Arm1);

                m_purifiers.Add(purifier);
            }

            OutputArm = new Arm(this, new Vector2(m_size * 2 + 3, 0), HexRotation.R180, MechanismType.Arm1, extension: 3);
        }

        public override void PrepareToGenerate(Element element)
        {
            m_targetMetal = element;
        }

        public override void Consume(Element element, int id)
        {
            m_numPurifiers = PeriodicTable.GetMetalDifference(element, m_targetMetal);
            m_consumeCoroutine.Next();
        }

        private IEnumerable<object> ConsumeMetal()
        {
            for (int iteration = 1; iteration <= (1 << m_numPurifiers); iteration++)
            {
                if (iteration % 2 == 1)
                {
                    Writer.WriteGrabResetAction(m_purifiers[0].BigArm, Instruction.Retract);
                    yield return null;
                    continue;
                }

                // Allow time for the atoms to transmute
                Writer.AdjustTime(1);

                int power = 4;
                for (int purifier = 0; purifier < m_numPurifiers - 1; purifier++)
                {
                    if (iteration % power == power / 2)
                    {
                        // Move to the far input cell of the purifier
                        Writer.WriteGrabResetAction(m_purifiers[purifier].SmallArm, new[] { Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise });
                        Writer.AdjustTime(1);
                    }
                    else if (iteration % power == 0)
                    {
                        // Move to the near input cell of the purifier
                        Writer.WriteGrabResetAction(m_purifiers[purifier].SmallArm, Instruction.RotateCounterclockwise);
                        Writer.AdjustTime(1);
                    }

                    power *= 2;
                }

                yield return null;
            }
        }

        public override void Generate(Element element, int id)
        {
            // Move the completed metal to the end of the last used purifier
            Writer.WriteGrabResetAction(m_purifiers[m_numPurifiers - 1].SmallArm, Instruction.RotateCounterclockwise);

            // Move past unused purifiers
            PassThroughAtom(m_numPurifiers);

            m_consumeCoroutine.Reset();
        }

        public override void PassThrough(Element element)
        {
            PassThroughAtom(0);
        }

        private void PassThroughAtom(int firstPurifier)
        {
            for (int purifier = firstPurifier; purifier < m_size; purifier++)
            {
                Writer.WriteGrabResetAction(m_purifiers[purifier].BigArm, Instruction.RotateCounterclockwise);
            }

            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }
    }
}
