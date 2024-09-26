using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard.Input
{
    /// <summary>
    /// Dissassembles a non-linear molecule with 3 atoms in a bent shape:
    /// O - O
    ///      \
    ///       O
    /// </summary>
    public class NonLinear3BentDisassembler : MoleculeDisassembler
    {
        public override int Height => 5;
        public override int HeightBelowOrigin => 1;

        private Arm m_grabArm;
        private Arm m_middleArm;
        private Arm m_outputArm;

        private LoopingCoroutine<object> m_extractAtomsCoroutine;

        public NonLinear3BentDisassembler(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position, molecule)
        {
            Molecule = molecule;
            if (!IsCompatible(molecule))
            {
                throw new ArgumentException($"Molecule is not compatible with {nameof(NonLinear3BentDisassembler)}: {molecule}");
            }

            if (!IsCorrectOrientation(molecule))
            {
                throw new ArgumentException($"Molecule has incorrect orientation: {molecule}");
            }

            m_extractAtomsCoroutine = new LoopingCoroutine<object>(ExtractAtoms);

            var reagentPos = new Vector2(-6, -1);
            new Reagent(this, reagentPos, HexRotation.R0, molecule);
            m_grabArm = new Arm(this, reagentPos.Add(1, 2), HexRotation.R240, ArmType.Arm1);
            m_middleArm = new Arm(this, reagentPos.Add(2, 2), HexRotation.R240, ArmType.Arm1);
            m_outputArm = new Arm(this, new Vector2(-3, 3), HexRotation.R240, ArmType.Arm1, extension: 3);

            new Glyph(this, new Vector2(-4, 0), HexRotation.R0, GlyphType.Unbonding);
        }

        public static bool IsCompatible(Molecule molecule)
        {
            return molecule.Atoms.Count() == 3 && molecule.Size == 3;
        }

        private static bool IsCorrectOrientation(Molecule molecule)
        {
            return molecule.GetAtom(new Vector2(0, 1)) != null
                && molecule.GetAtom(new Vector2(1, 1)) != null
                && molecule.GetAtom(new Vector2(2, 0)) != null;
        }

        public static void PrepareMolecule(Molecule molecule)
        {
            // Make sure the molecule has this orientation:
            /// O - O
            ///      \
            ///       O
            foreach (var _ in HexRotation.All)
            {
                if (IsCorrectOrientation(molecule))
                {
                    return;
                }

                molecule.Rotate60Clockwise();
            }

            throw new SolverException($"Unexpected molecule shape for {nameof(NonLinear3BentDisassembler)}: {molecule}");
        }

        public static IEnumerable<Element> GetElementInputOrder(Molecule molecule)
        {
            yield return molecule.GetAtom(new Vector2(2, 0)).Element;
            yield return molecule.GetAtom(new Vector2(0, 1)).Element;
            yield return molecule.GetAtom(new Vector2(1, 1)).Element;
        }

        public override void GenerateNextAtom()
        {
            m_extractAtomsCoroutine.Next();
        }

        private IEnumerable<object> ExtractAtoms()
        {
            Writer.NewFragment();
            Writer.Write(m_grabArm, [Instruction.Grab, Instruction.RotateCounterclockwise]);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;

            Writer.NewFragment();
            Writer.Write(m_grabArm, [Instruction.PivotCounterclockwise, Instruction.PivotCounterclockwise, Instruction.Reset]);
            Writer.AdjustTime(-1);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;

            Writer.NewFragment();
            Writer.WriteGrabResetAction(m_middleArm, Instruction.RotateCounterclockwise);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;
        }
    }
}
