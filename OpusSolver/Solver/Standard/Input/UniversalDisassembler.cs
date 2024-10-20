using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard.Input
{
    /// <summary>
    /// Dissassembles an arbitrary molecule into single atoms.
    /// </summary>
    public class UniversalDisassembler : MoleculeDisassembler
    {
        public override int Height => Molecule.Height + 4;
        public override int HeightBelowOrigin => Molecule.Height - 1;

        private readonly int m_unbondWidth;
        private Arm m_grabArm;
        private List<Arm> m_lowerUnbondArms;
        private List<Arm> m_upperUnbondArms;
        private List<Arm> m_moveArms;

        private bool m_addVerticalOffsetAtStart;

        private LoopingCoroutine<Element> m_extractAtomsCoroutine;

        public UniversalDisassembler(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position, molecule)
        {
            m_addVerticalOffsetAtStart = IsProblematicMolecule(molecule);

            m_extractAtomsCoroutine = new LoopingCoroutine<Element>(ExtractAtoms);

            // The atoms need to be moved at least 3 spaces to fully unbond them
            m_unbondWidth = Math.Max(3, Molecule.Width);

            var reagentOffset = m_addVerticalOffsetAtStart ? new Vector2(0, -1) : new Vector2();
            var reagentPos = new Vector2(-Molecule.Width * 2 - m_unbondWidth - 2, -molecule.Height + 1);
            new Reagent(this, reagentPos + reagentOffset, HexRotation.R0, molecule);

            var armPos = AddArms(reagentPos);
            AddTracks(armPos);
            AddGlyphs();
        }

        /// <summary>
        /// Checks if the molecule has a shape which causes a collision with the next reagent as it's moved horizontally
        /// off the input area. e.g.
        /// 
        ///        Sa--Ai      Sa
        ///             \       \
        ///              Wa      Sa
        ///               \     /
        ///    Sa--Sa--Sa--Sa--Sa
        ///   /     \     /   /
        ///  Sa      Sa--Fi  Sa      Sa
        ///         / \   \ /       /
        ///        Wa  Sa  Sa--Wa--Ai
        ///       /     \ /
        ///      Ai      Sa
        ///       \       \
        ///        Sa      Sa--Sa
        /// </summary>
        private bool IsProblematicMolecule(Molecule molecule)
        {
            if (molecule.Width > 2)
            {
                for (int y = 1; y < molecule.Height - 1; y++)
                {
                    if (molecule.GetAtom(new(molecule.Width - 1, y)) != null && molecule.GetAtom(new(molecule.Width - 2, y)) == null
                        && molecule.GetAtom(new(molecule.Width - 1, y - 1)) != null && molecule.GetAtom(new(molecule.Width - 2, y - 1)) != null
                        && molecule.GetAtom(new(molecule.Width - 1, y + 1)) == null && molecule.GetAtom(new(molecule.Width - 2, y + 1)) == null
                        && molecule.GetAtom(new(0, y)) != null && molecule.GetAtom(new(1, y)) == null
                        && molecule.GetAtom(new(0, y + 1)) != null && molecule.GetAtom(new(1, y + 1)) != null
                        && molecule.GetAtom(new(0, y - 1)) == null && molecule.GetAtom(new(1, y - 1)) == null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Vector2 AddArms(Vector2 reagentPos)
        {
            int grabX = Molecule.GetRow(Molecule.Height - 1).Last().Position.X;
            var armPos = reagentPos.Add(grabX, Molecule.Height + 2);

            var grabArmPos = armPos;
            if (m_addVerticalOffsetAtStart)
            {
                grabArmPos.Y -= 1;
            }

            m_grabArm = new Arm(this, grabArmPos, HexRotation.R240, ArmType.Arm1, extension: 3);
            if (m_addVerticalOffsetAtStart)
            {
                new Track(this, grabArmPos, [new(HexRotation.R60, 1), new(HexRotation.R0, Molecule.Width)]);
            }
            else
            {
                new Track(this, armPos, HexRotation.R0, Molecule.Width);
            }

            armPos = reagentPos.Add(Molecule.Width, Molecule.Height);
            m_lowerUnbondArms = Enumerable.Range(0, Molecule.Width).Select(x => new Arm(this, armPos.Add(x, 0), HexRotation.R240, ArmType.Piston, extension: 2)).ToList();
            m_upperUnbondArms = Enumerable.Range(0, Molecule.Width).Select(x => new Arm(this, armPos.Add(x, 1), HexRotation.R240, ArmType.Arm1, extension: 2)).ToList();
            m_moveArms = Enumerable.Range(0, Molecule.Width).Select(x => new Arm(this, armPos.Add(m_unbondWidth + x, 2), HexRotation.R240, ArmType.Arm1, extension: 3)).ToList();

            return armPos;
        }

        private void AddTracks(Vector2 armPos)
        {
            new Track(this, armPos, HexRotation.R0, Molecule.Width + m_unbondWidth - 1);
            new Track(this, armPos.Add(0, 1), HexRotation.R0, Molecule.Width + m_unbondWidth - 1);

            // For the "move arms", make the path wrap around to the left, to avoid it encroaching onto the output area
            var path = new[] {
                 new Track.Segment { Direction = HexRotation.R0, Length = Molecule.Width - 1 },
                 new Track.Segment { Direction = HexRotation.R60, Length = 1 },
                 new Track.Segment { Direction = HexRotation.R180, Length = Molecule.Width - 2 }
            };
            new Track(this, armPos.Add(m_unbondWidth, 2), path);
        }

        private void AddGlyphs()
        {
            var glyphPos = new Vector2(-m_unbondWidth - 3, 0);
            new Glyph(this, glyphPos, HexRotation.R240, GlyphType.Unbonding);
            new Glyph(this, glyphPos.Add(1, 0), HexRotation.R300, GlyphType.Unbonding);
            new Glyph(this, glyphPos.Add(2, 0), HexRotation.R0, GlyphType.Unbonding);
        }

        public override void GenerateNextAtom()
        {
            m_extractAtomsCoroutine.Next();
        }

        private IEnumerable<Element> ExtractAtoms()
        {
            Writer.NewFragment();

            Writer.WriteGrabResetAction(m_grabArm, Enumerable.Repeat(Instruction.MovePositive, Molecule.Width + (m_addVerticalOffsetAtStart ? 1 : 0)));

            var moveLower = Enumerable.Repeat(Instruction.MovePositive, m_unbondWidth).Concat(Enumerable.Repeat(Instruction.MoveNegative, m_unbondWidth));
            var lowerArmsGrab = new[] { Instruction.Grab }.Concat(moveLower).Concat([Instruction.Retract, Instruction.Drop, Instruction.Reset]).ToList();
            var lowerArmsWait = new[] { Instruction.Wait }.Concat(moveLower).Concat([Instruction.Wait,    Instruction.Wait, Instruction.Reset]).ToList();

            var moveUpper = Enumerable.Repeat(Instruction.MovePositive, m_unbondWidth);
            var upperArmsGrab = new[] { Instruction.Grab }.Concat(moveUpper).Concat([Instruction.Drop, Instruction.Reset]);
            var upperArmsWait = new[] { Instruction.Wait }.Concat(moveUpper).Concat([Instruction.Wait, Instruction.Reset]);

            for (int y = Molecule.Height - 1; y >= 0; y--)
            {
                if (y > 0)
                {
                    // To save instructions and potentially cycles, only move the arms for the first atom and later. We have
                    // to move all arms to the right of the first arm that moves, otherwise they'll collide with each other.
                    int firstLowerAtomX = Molecule.GetRow(y - 1).First().Position.X;
                    for (int x = firstLowerAtomX; x < Molecule.Width; x++)
                    {
                        // We need to move all the arms so that they don't hit each other, but we only write Grab instructions
                        // if there's actually an atom there. This way, the arm can potentially be optimized away later on if
                        // it never actually grabs anything.
                        bool isAtomPresent = Molecule.GetAtom(new Vector2(x, y - 1)) != null;
                        Writer.Write(m_lowerUnbondArms[x], isAtomPresent ? lowerArmsGrab : lowerArmsWait, updateTime: false);
                    }
                }

                int firstUpperAtomX = Molecule.GetRow(y).First().Position.X;
                for (int x = firstUpperAtomX; x < Molecule.Width; x++)
                {
                    bool isAtomPresent = Molecule.GetAtom(new Vector2(x, y)) != null;
                    Writer.Write(m_upperUnbondArms[x], isAtomPresent ? upperArmsGrab : upperArmsWait, updateTime: false);
                }

                // Move to just after the upper arms finish moving (i.e. when the unbonded atoms have been dropped)
                Writer.AdjustTime(moveUpper.Count() + 1);

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
