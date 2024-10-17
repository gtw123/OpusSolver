using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Input.Complex
{
    public class ComplexDisassembler : LowCostAtomGenerator
    {
        private IEnumerable<MoleculeDismantler> m_dismantlers;
        private readonly Dictionary<int, LoopingCoroutine<object>> m_disassembleCoroutines;
        private readonly Glyph m_unbonder;

        private MoleculeInput m_input;

        public const int MaxReagents = 1;

        public override int RequiredWidth => 2;

        private static readonly Transform2D LowerUnbonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D UpperUnbonderPosition = new Transform2D(new Vector2(-1, 1), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [LowerUnbonderPosition, UpperUnbonderPosition];

        /// <summary>
        /// The direction in which this disassembler's unbonder removes bonds, from the previous atom to the new atom.
        /// </summary>
        public static HexRotation UnbondingDirection = HexRotation.R300;

        public ComplexDisassembler(ProgramWriter writer, ArmArea armArea, IEnumerable<MoleculeDismantler> dismantlers)
            : base(writer, armArea)
        {
            m_dismantlers = dismantlers;
            m_disassembleCoroutines = dismantlers.ToDictionary(d => d.Molecule.ID, d => new LoopingCoroutine<object>(() => Disassemble(d)));
            m_unbonder = new Glyph(this, LowerUnbonderPosition.Position, UnbondingDirection - HexRotation.R180, GlyphType.Unbonding);

            if (m_dismantlers.Count() > MaxReagents)
            {
                throw new UnsupportedException($"{nameof(ComplexDisassembler)} currently only supports {MaxReagents} reagents (requested {m_dismantlers.Count()}).");
            }

            var dismantler = m_dismantlers.First();
            var molecule = dismantler.Molecule;
            var atomToGrab = FindAtomToGrab(molecule);

            var moleculeTransform = new Transform2D(LowerUnbonderPosition.Position - atomToGrab.Position, HexRotation.R0);
            var armPos = LowerUnbonderPosition.Position - new Vector2(ArmArea.ArmLength, 0);
            var inputTransform = moleculeTransform.RotateAbout(armPos, -HexRotation.R60);

            m_input = new MoleculeInput(this, Writer, ArmArea, inputTransform, molecule, new Transform2D());
        }

        private Atom FindAtomToGrab(Molecule molecule)
        {
            for (int x = 0; x < molecule.Width; x++)
            {
                for (int y = 0; y <= x; y++)
                {
                    var atomToGrab = molecule.GetAtom(new(x - y, y));
                    if (atomToGrab != null)
                    {
                        return atomToGrab;
                    }
                }
            }

            throw new SolverException("Couldn't find an atom on the molecule to grab.");
        }

        public override void Generate(Element element, int id)
        {
            m_disassembleCoroutines[id].Next();
        }

        private IEnumerable<object> Disassemble(MoleculeDismantler dismantler)
        {
            AtomCollection remainingAtoms = null;

            for (int opIndex = 0; opIndex < dismantler.Operations.Count - 1; opIndex++)
            {
                if (opIndex == 0)
                {
                    remainingAtoms = m_input.GrabMolecule();
                }
                else
                {
                    ArmController.SetMoleculeToGrab(remainingAtoms);
                }

                var op = dismantler.Operations[opIndex];

                remainingAtoms.TargetMolecule = remainingAtoms.Copy();
                remainingAtoms.TargetMolecule.RemoveBond(op.Atom.Position, op.NextAtom.Position);

                var targetUnbondPosition = UpperUnbonderPosition.Position;
                var targetTransform = new Transform2D(targetUnbondPosition - op.Atom.Position, HexRotation.R0);
                targetTransform = targetTransform.RotateAbout(targetUnbondPosition, op.MoleculeRotation);
                if (!ArmController.MoveMoleculeTo(targetTransform, this, options: new ArmMovementOptions { AllowUnbonding = true }, throwOnFailure: false))
                {
                    targetUnbondPosition = LowerUnbonderPosition.Position;
                    targetTransform = new Transform2D(targetUnbondPosition - op.Atom.Position, HexRotation.R0);
                    targetTransform = targetTransform.RotateAbout(targetUnbondPosition, op.MoleculeRotation + HexRotation.R180);
                    ArmController.MoveMoleculeTo(targetTransform, this, options: new ArmMovementOptions { AllowUnbonding = true });
                }

                // Make sure we're grabbing the atom we want to move
                if (ArmController.GetGrabberPosition() != GetWorldTransform().Apply(targetUnbondPosition))
                {
                    var molecule = ArmController.DropMolecule();
                    var removedAtom = molecule.RemoveAtom(molecule.GetAtomAtWorldPosition(targetUnbondPosition, this));
                    ArmController.SetMoleculeToGrab(removedAtom);

                    remainingAtoms = molecule;
                }
                else
                {
                    remainingAtoms = ArmController.UnbondGrabbedAtomFromOthers();
                }

                yield return null;
            }

            // Recreate the atom collection so we can be sure it's at (0, 0) and with no bonds
            remainingAtoms = new AtomCollection(remainingAtoms.Atoms[0].Element, GetWorldTransform().Apply(LowerUnbonderPosition));
            ArmController.SetMoleculeToGrab(remainingAtoms);
            yield return null;
        }

        public static IEnumerable<MoleculeDismantler> CreateMoleculeDismantlers(IEnumerable<Molecule> reagents)
        {
            return reagents.Select(p => new MoleculeDismantler(p)).ToList();
        }
    }
}
