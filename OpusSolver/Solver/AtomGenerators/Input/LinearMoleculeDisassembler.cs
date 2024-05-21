using System;
using System.Collections.Generic;
using static System.FormattableString;

namespace OpusSolver.Solver.AtomGenerators.Input
{
    /// <summary>
    /// Dissassembles a multi-atom but linear molecule into single atoms.
    /// </summary>
    public class LinearMoleculeDisassembler : MoleculeDisassembler
    {
        public override int Height => 4;

        private Arm m_grabArm;
        private Arm m_outputArm;

        private LoopingCoroutine<Element> m_extractAtomsCoroutine;

        public LinearMoleculeDisassembler(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position, molecule)
        {
            Molecule = molecule;
            if (molecule.Height != 1)
            {
                throw new ArgumentException(Invariant($"Molecule must have height 1. Specified height: {molecule.Height}."), "molecule");
            }

            m_extractAtomsCoroutine = new LoopingCoroutine<Element>(ExtractAtoms);

            var reagentPos = new Vector2(-Molecule.Width - 2, 1);
            new Reagent(this, reagentPos.Add(molecule.Origin), molecule.Rotation, molecule.ID);
            m_grabArm = new Arm(this, reagentPos.Add(0, 1), Direction.SW, MechanismType.Piston);
            m_outputArm = new Arm(this, new Vector2(-3, 3), Direction.SW, MechanismType.Arm1, extension: 3);

            new Track(this, m_grabArm.Position, Direction.E, Molecule.Width - 1);
            new Glyph(this, new Vector2(-4, 0), Direction.E, GlyphType.Unbonding);
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
