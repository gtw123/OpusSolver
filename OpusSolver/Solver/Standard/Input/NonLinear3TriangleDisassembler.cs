using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard.Input
{
    /// <summary>
    /// Dissassembles a non-linear molecule with 3 atoms in a triangle shape:
    ///     O         O
    ///    / \   or  / \
    ///   O   O     O - O
    /// </summary>
    public class NonLinear3TriangleDisassembler : MoleculeDisassembler
    {
        public override int Height => 4;
        public override int HeightBelowOrigin => 1;

        private Arm m_grabArm;
        private Arm m_outputArm;

        private LoopingCoroutine<object> m_extractAtomsCoroutine;

        public NonLinear3TriangleDisassembler(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position, molecule)
        {
            Molecule = molecule;
            if (!IsCompatible(molecule))
            {
                throw new ArgumentException($"Molecule is not compatible with {nameof(NonLinear3TriangleDisassembler)}: {molecule}");
            }

            if (!IsCorrectOrientation(molecule))
            {
                throw new ArgumentException($"Molecule has incorrect orientation: {molecule}");
            }

            bool hasAllBonds = HasAllBonds(molecule);
            m_extractAtomsCoroutine = new LoopingCoroutine<object>(hasAllBonds ? ExtractAtomsAllBonds : ExtractAtomsTwoBonds);

            var reagentPos = new Vector2(-6, 0);
            new Reagent(this, reagentPos, HexRotation.R0, molecule);
            m_grabArm = new Arm(this, reagentPos.Add(0, 3), HexRotation.R240, ArmType.Arm1, extension: hasAllBonds ? 3 : 2);
            m_outputArm = new Arm(this, new Vector2(-3, 3), HexRotation.R240, ArmType.Arm1, extension: 3);

            new Glyph(this, new Vector2(-3, 0), HexRotation.R120, GlyphType.Unbonding);
            new Track(this, m_grabArm.Transform.Position, hasAllBonds ? HexRotation.R120 : HexRotation.R300, 1);
        }

        public static bool IsCompatible(Molecule molecule)
        {
            return molecule.Atoms.Count() == 3 && molecule.Size == 2;
        }

        private static bool IsCorrectOrientation(Molecule molecule)
        {
            var atom = molecule.GetAtom(new Vector2(0, 0));
            if (atom == null || atom.Bonds[HexRotation.R60] == BondType.None)
            {
                return false;
            }

            atom = molecule.GetAtom(new Vector2(1, 0));
            if (atom == null || atom.Bonds[HexRotation.R120] == BondType.None)
            {
                return false;
            }

            return true;
        }

        public static void PrepareMolecule(Molecule molecule)
        {
            // Make sure the molecule has this orientation:
            ///     O         O
            ///    / \   or  / \
            ///   O   O     O - O
            foreach (var _ in HexRotation.All)
            {
                if (IsCorrectOrientation(molecule))
                {
                    return;
                }

                molecule.Rotate60Clockwise();
            }

            throw new ArgumentException($"Unexpected molecule shape for {nameof(NonLinear3TriangleDisassembler)}: {molecule}");
        }

        private static bool HasAllBonds(Molecule molecule)
        {
            return molecule.GetAtom(new Vector2(0, 0)).Bonds[HexRotation.R0] != BondType.None;
        }

        public static IEnumerable<Element> GetElementInputOrder(Molecule molecule)
        {
            yield return molecule.GetAtom(new Vector2(0, 0)).Element;

            if (HasAllBonds(molecule))
            {
                yield return molecule.GetAtom(new Vector2(0, 1)).Element;
                yield return molecule.GetAtom(new Vector2(1, 0)).Element;
            }
            else
            {
                yield return molecule.GetAtom(new Vector2(1, 0)).Element;
                yield return molecule.GetAtom(new Vector2(0, 1)).Element;
            }
        }

        public override void GenerateNextAtom()
        {
            m_extractAtomsCoroutine.Next();
        }

        private IEnumerable<object> ExtractAtomsTwoBonds()
        {
            Writer.NewFragment();
            Writer.Write(m_grabArm, [Instruction.Grab, Instruction.RotateCounterclockwise]);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;

            Writer.NewFragment();
            Writer.Write(m_grabArm, Instruction.PivotClockwise);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;

            Writer.NewFragment();
            Writer.Write(m_grabArm, [Instruction.MovePositive, Instruction.Reset]);
            Writer.AdjustTime(-1);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;
        }

        private IEnumerable<object> ExtractAtomsAllBonds()
        {
            Writer.NewFragment();
            Writer.Write(m_grabArm, [Instruction.Grab, Instruction.RotateCounterclockwise, Instruction.PivotCounterclockwise, Instruction.Drop]);
            Writer.AdjustTime(-1);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;

            Writer.NewFragment();
            Writer.Write(m_grabArm, [Instruction.MovePositive, Instruction.Grab, Instruction.PivotCounterclockwise]);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;

            Writer.NewFragment();
            Writer.Write(m_grabArm, [Instruction.MoveNegative, Instruction.Reset]);
            Writer.AdjustTime(-1);
            Writer.WriteGrabResetAction(m_outputArm, Instruction.RotateCounterclockwise);
            yield return null;
        }
    }
}
