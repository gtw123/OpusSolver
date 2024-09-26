using OpusSolver.Solver.ElementGenerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard
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
            public Element? CurrentElement;
        }

        private List<Purifier> m_purifiers = new List<Purifier>();

        private IReadOnlyList<MetalPurifierGenerator.PurificationSequence> m_sequences;
        private readonly int m_size;

        public MetalPurifier(ProgramWriter writer, IReadOnlyList<MetalPurifierGenerator.PurificationSequence> sequences)
            : base(writer)
        {
            m_sequences = sequences;
            m_size = sequences.Max(s => PeriodicTable.GetMetalDifference(s.LowestMetalUsed, s.TargetMetal));

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
                var armType = (i == 0) ? ArmType.Piston : ArmType.Arm1;
                purifier.BigArm = new Arm(this, pos.Add(0, 1), HexRotation.R240, armType, extension: 2);
                purifier.SmallArm = new Arm(this, pos.Add(1, 0), HexRotation.R240, ArmType.Arm1);

                m_purifiers.Add(purifier);
            }

            OutputArm = new Arm(this, new Vector2(m_size * 2 + 3, 0), HexRotation.R180, ArmType.Arm1, extension: 3);
        }

        public override void Consume(Element element, int id)
        {
            var sequence = m_sequences[id];
            int purifierIndex = element - sequence.LowestMetalUsed;

            // Move the atom to the target purifier
            for (int index = 0; index < purifierIndex; index++)
            {
                Writer.WriteGrabResetAction(m_purifiers[index].BigArm, Instruction.RotateCounterclockwise);
            }

            var purifier = m_purifiers[purifierIndex];
            if (purifier.CurrentElement == null)
            {
                // Move to the far input cell of the purifier
                if (purifierIndex == 0)
                {
                    // The first purifier uses a piston to move atoms to the other part of the glyph
                    Writer.WriteGrabResetAction(purifier.BigArm, Instruction.Retract);
                }
                else
                {
                    // Use the previous purifier's small arm to move the atom to the correct location
                    var previousPurifier = m_purifiers[purifierIndex - 1];
                    Writer.AdjustTime(-1);
                    Writer.Write(previousPurifier.SmallArm, Instruction.RotateCounterclockwise);
                    Writer.WriteGrabResetAction(previousPurifier.SmallArm, Instruction.RotateCounterclockwise);
                }

                purifier.CurrentElement = element;
                return;
            }

            // The purifier atom now has two atoms, so it will transmute to a new atom. We may then need
            // to move that onto the next purifier.
            int finalPurifierIndex = sequence.TargetMetal - sequence.LowestMetalUsed - 1;
            while (purifierIndex <= finalPurifierIndex)
            {
                // Wait for the atoms to transmute
                Writer.Write(purifier.SmallArm, Instruction.Wait);
                purifier.CurrentElement = null;

                if (purifierIndex == finalPurifierIndex)
                {
                    // Move the atom to the end of this purifier
                    Writer.WriteGrabResetAction(purifier.SmallArm, Instruction.RotateCounterclockwise);
                    break;
                }

                // Move the generated atom to the next purifier
                var nextPurifier = m_purifiers[purifierIndex + 1];
                if (nextPurifier.CurrentElement == null)
                {
                    // Move to the far input cell of the purifier
                    Writer.WriteGrabResetAction(purifier.SmallArm, [Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise]);
                    nextPurifier.CurrentElement = element;
                    break;
                }
                else
                {
                    // Move to the near input cell of the purifier
                    Writer.WriteGrabResetAction(purifier.SmallArm, Instruction.RotateCounterclockwise);
                }

                purifierIndex++;
                purifier = m_purifiers[purifierIndex];
            }
        }

        public override void Generate(Element element, int id)
        {
            if (m_purifiers.Any(p => p.CurrentElement.HasValue))
            {
                throw new SolverException("Expected all purifiers to be empty but some still had elements.");
            }

            var sequence = m_sequences[id];
            if (element != sequence.TargetMetal)
            {
                throw new SolverException($"Expected to generate an element of {sequence.TargetMetal} but {element} was requested.");
            }

            // Move the atom past the unused purifiers
            int purifierIndex = element - sequence.LowestMetalUsed;
            PassThroughAtom(purifierIndex);
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
