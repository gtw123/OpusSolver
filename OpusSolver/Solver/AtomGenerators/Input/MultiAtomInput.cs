using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Input
{
    /// <summary>
    /// Decomposes a multi-atom molecule into single atoms.
    /// </summary>
    public class MultiAtomInput : MoleculeInput
    {
        public override int Height => Molecule.Height + 4;
        public override int HeightBelowOrigin => Molecule.Height - 1;

        private readonly int m_unbondWidth;
        private Arm m_grabArm;
        private List<Arm> m_lowerUnbondArms;
        private List<Arm> m_upperUnbondArms;
        private List<Arm> m_moveArms;

        private LoopingCoroutine<Element> m_extractAtomsCoroutine;

        public MultiAtomInput(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position, molecule)
        {
            m_extractAtomsCoroutine = new LoopingCoroutine<Element>(ExtractAtoms);

            // The atoms need to be moved at least 3 spaces to fully unbond them
            m_unbondWidth = Math.Max(3, Molecule.Width);

            var reagentPos = new Vector2(-Molecule.Width * 2 - m_unbondWidth - 2, -molecule.Height + 1);
            new Reagent(this, reagentPos.Add(molecule.Origin), molecule.Rotation, molecule.ID);

            var armPos = AddArms(reagentPos);
            AddTracks(armPos);
            AddGlyphs();
        }

        private Vector2 AddArms(Vector2 reagentPos)
        {
            int grabX = Molecule.GetRow(Molecule.Height - 1).Last().Position.X;
            var armPos = reagentPos.Add(grabX, Molecule.Height + 2);

            m_grabArm = new Arm(this, armPos, Direction.SW, MechanismType.Arm1, extension: 3);
            new Track(this, armPos, Direction.E, Molecule.Width);

            armPos = reagentPos.Add(Molecule.Width, Molecule.Height);
            m_lowerUnbondArms = Enumerable.Range(0, Molecule.Width).Select(x => new Arm(this, armPos.Add(x, 0), Direction.SW, MechanismType.Piston, extension: 2)).ToList();
            m_upperUnbondArms = Enumerable.Range(0, Molecule.Width).Select(x => new Arm(this, armPos.Add(x, 1), Direction.SW, MechanismType.Arm1, extension: 2)).ToList();
            m_moveArms = Enumerable.Range(0, Molecule.Width).Select(x => new Arm(this, armPos.Add(m_unbondWidth + x, 2), Direction.SW, MechanismType.Arm1, extension: 3)).ToList();

            return armPos;
        }

        private void AddTracks(Vector2 armPos)
        {
            new Track(this, armPos, Direction.E, Molecule.Width + m_unbondWidth - 1);
            new Track(this, armPos.Add(0, 1), Direction.E, Molecule.Width + m_unbondWidth - 1);

            // For the "move arms", make the path wrap around to the left, to avoid it encroaching onto the output area
            var path = new[] {
                 new Track.Segment { Direction = Direction.E, Length = Molecule.Width - 1 },
                 new Track.Segment { Direction = Direction.NE, Length = 1 },
                 new Track.Segment { Direction = Direction.W, Length = Molecule.Width - 2 }
            };
            new Track(this, armPos.Add(m_unbondWidth, 2), path);
        }

        private void AddGlyphs()
        {
            var glyphPos = new Vector2(-m_unbondWidth - 3, 0);
            new Glyph(this, glyphPos, Direction.SW, GlyphType.Unbonding);
            new Glyph(this, glyphPos.Add(1, 0), Direction.SE, GlyphType.Unbonding);
            new Glyph(this, glyphPos.Add(2, 0), Direction.E, GlyphType.Unbonding);
        }

        public override Element GetNextAtom()
        {
            return m_extractAtomsCoroutine.Next();
        }

        private IEnumerable<Element> ExtractAtoms()
        {
            Writer.NewFragment();

            Writer.WriteGrabResetAction(m_grabArm, Enumerable.Repeat(Instruction.MovePositive, Molecule.Width));

            var movePos = Enumerable.Repeat(Instruction.MovePositive, m_unbondWidth);
            var moveNeg = Enumerable.Repeat(Instruction.MoveNegative, m_unbondWidth);
            for (int y = Molecule.Height - 1; y >= 0; y--)
            {
                Writer.WriteGrabResetAction(m_lowerUnbondArms, movePos.Concat(moveNeg).Concat(new[] { Instruction.Retract }), updateTime: false);
                Writer.WriteGrabResetAction(m_upperUnbondArms, movePos);

                int lastAtomX = Molecule.GetRow(y).First().Position.X;
                for (int x = Molecule.Width - 1; x >= lastAtomX; x--)
                {
                    if (x == Molecule.Width - 1)
                    {
                        Writer.Write(m_moveArms, Instruction.Grab);
                    }
                    else
                    {
                        Writer.Write(m_moveArms, Instruction.MovePositive);
                    }

                    var atom = Molecule.GetAtom(new Vector2(x, y));
                    if (atom != null)
                    {
                        Writer.Write(m_moveArms[x], new[] { Instruction.RotateCounterclockwise, Instruction.Drop });

                        if (x == lastAtomX)
                        {
                            Writer.Write(m_moveArms, Instruction.Reset, updateTime: false);
                        }

                        Writer.AdjustTime(-1);
                        yield return atom.Element;

                        Writer.NewFragment();
                    }
                }
            }
        }
    }
}
