﻿using System;
using System.Collections.Generic;
using static System.FormattableString;

namespace OpusSolver.Solver.AtomGenerators.Input.Dissassemblers
{
    /// <summary>
    /// Dissassembles a multi-atom but linear molecule into single atoms.
    /// </summary>
    public class LinearDisassembler : MoleculeDisassembler
    {
        public override int Height => 4;

        private Arm m_grabArm;
        private Arm m_outputArm;

        private LoopingCoroutine<Element> m_extractAtomsCoroutine;

        public LinearDisassembler(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position, molecule)
        {
            Molecule = molecule;
            if (molecule.Height != 1)
            {
                throw new ArgumentException(Invariant($"Molecule must have height 1. Specified height: {molecule.Height}."), "molecule");
            }

            m_extractAtomsCoroutine = new LoopingCoroutine<Element>(ExtractAtoms);

            var reagentPos = new Vector2(-Molecule.Width - 2, 1);
            new Reagent(this, reagentPos, HexRotation.R0, molecule);
            m_grabArm = new Arm(this, reagentPos.Add(0, 1), HexRotation.R240, ArmType.Piston);
            m_outputArm = new Arm(this, new Vector2(-3, 3), HexRotation.R240, ArmType.Arm1, extension: 3);

            new Track(this, m_grabArm.Transform.Position, HexRotation.R0, Molecule.Width - 1);
            new Glyph(this, new Vector2(-4, 0), HexRotation.R0, GlyphType.Unbonding);
        }

        public override Element GetNextAtom()
        {
            return m_extractAtomsCoroutine.Next();
        }

        private IEnumerable<Element> ExtractAtoms()
        {
            Writer.NewFragment();
            Writer.Write(m_grabArm, new[] { Instruction.Grab, Instruction.Extend });
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return Molecule.GetAtom(new Vector2(0, 0)).Element;

            for (int x = 1; x < Molecule.Width; x++)
            {
                Writer.NewFragment();
                Writer.Write(m_grabArm, Instruction.MovePositive);
                if (x == Molecule.Width - 1)
                {
                    Writer.Write(m_grabArm, Instruction.Reset, updateTime: false);
                }

                Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
                yield return Molecule.GetAtom(new Vector2(x, 0)).Element;
            }
        }
    }
}