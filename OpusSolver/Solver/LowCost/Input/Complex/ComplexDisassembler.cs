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

            var moleculeTransform = new Transform2D(LowerUnbonderPosition.Position, HexRotation.R0);
            var armPos = LowerUnbonderPosition.Position - new Vector2(ArmArea.ArmLength, 0);
            var inputTransform = moleculeTransform.RotateAbout(armPos, -HexRotation.R60);

            m_input = new MoleculeInput(this, Writer, ArmArea, inputTransform, dismantler.Molecule, new Transform2D());
        }

        public override void Generate(Element element, int id)
        {
            m_disassembleCoroutines[id].Next();
        }

        private IEnumerable<object> Disassemble(MoleculeDismantler dismantler)
        {
            AtomCollection remainingAtoms = null;

            for (int opIndex = 0; opIndex < dismantler.Operations.Count; opIndex++)
            {
                if (opIndex == 0)
                {
                    remainingAtoms = m_input.GrabMolecule();
                }
                else if (opIndex == dismantler.Operations.Count - 1)
                {
                    // Recreate the atom collection so we can be sure it's at (0, 0) and with no bonds
                    remainingAtoms = new AtomCollection(remainingAtoms.Atoms[0].Element, GetWorldTransform().Apply(LowerUnbonderPosition));
                    ArmController.SetMoleculeToGrab(remainingAtoms);
                    yield return null;
                    yield break;
                }
                else
                {
                    ArmController.SetMoleculeToGrab(remainingAtoms);
                }

                // TODO: Should we start at index 1 instead of 0?

                var nextOp = dismantler.Operations[opIndex + 1];

                remainingAtoms.TargetMolecule = remainingAtoms.Copy();
                remainingAtoms.TargetMolecule.RemoveBond(nextOp.Atom.Position, nextOp.ParentAtom.Position);

                var targetTransform = new Transform2D(UpperUnbonderPosition.Position - nextOp.ParentAtom.Position, HexRotation.R0);
                targetTransform = targetTransform.RotateAbout(UpperUnbonderPosition.Position, nextOp.MoleculeRotation);
                ArmController.MoveMoleculeTo(targetTransform, this, options: new ArmMovementOptions { AllowUnbonding = true });

                // Make sure we're grabbing the atom we want to move
                if (ArmController.GetGrabberPosition() != GetWorldTransform().Apply(UpperUnbonderPosition).Position)
                {
                    var molecule = ArmController.DropMolecule();
                    var removedAtom = molecule.RemoveAtom(molecule.GetAtomAtWorldPosition(UpperUnbonderPosition.Position, this));
                    ArmController.SetMoleculeToGrab(removedAtom);

                    remainingAtoms = molecule;
                }
                else
                {
                    remainingAtoms = ArmController.UnbondGrabbedAtomFromOthers();
                }

                yield return null;
            }
        }

        public static IEnumerable<MoleculeDismantler> CreateMoleculeDismantlers(IEnumerable<Molecule> reagents)
        {
            return reagents.Select(p => new MoleculeDismantler(p)).ToList();
        }
    }
}
