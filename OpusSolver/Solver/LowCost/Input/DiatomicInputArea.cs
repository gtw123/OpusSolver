using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// An input area used when all reagents have at most two atoms.
    /// </summary>
    public class DiatomicInputArea : LowCostAtomGenerator
    {
        private record DisassemblerInfo(SimpleDisassembler Disassembler, int Index);

        private readonly Dictionary<int, DisassemblerInfo> m_disassemblers = new();

        private class StoredAtom
        {
            public Transform2D Transform;
            public Element Element;
            public int MoleculeID;
        }

        private StoredAtom m_unbondedAtom;
        private StoredAtom m_stashedAtom;

        public const int MaxReagents = 2;

        private static readonly Transform2D InnerUnbonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D OuterUnbonderPosition = new Transform2D(new Vector2(1, 0), HexRotation.R0);
        private readonly Transform2D m_stashPosition;
        private Transform2D m_input2GrabPosition;

        private readonly List<Transform2D> m_accessPoints = new();
        public override IEnumerable<Transform2D> RequiredAccessPoints => m_accessPoints;

        public DiatomicInputArea(ProgramWriter writer, ArmArea armArea, IEnumerable<Molecule> reagents)
            : base(writer, armArea)
        {
            if (reagents.Any(r => r.Atoms.Count() > 2))
            {
                throw new ArgumentException($"{nameof(DiatomicInputArea)} can't handle reagents with more than two atoms.");
            }

            if (reagents.Count() > MaxReagents)
            {
                throw new ArgumentException(Invariant($"{nameof(DiatomicInputArea)} can't handle more than {MaxReagents} distinct reagents."));
            }

            new Glyph(this, InnerUnbonderPosition.Position, HexRotation.R0, GlyphType.Unbonding);

            m_accessPoints.Add(OuterUnbonderPosition);
            m_accessPoints.Add(InnerUnbonderPosition);

            if (reagents.Count() >= 2)
            {
                m_stashPosition = new Transform2D(OuterUnbonderPosition.Position, HexRotation.R0).RotateAbout(OuterUnbonderPosition.Position - new Vector2(ArmArea.ArmLength, 0), HexRotation.R60);
                m_accessPoints.Add(m_stashPosition);
            }

            CreateDisassemblers(reagents);
        }

        private void CreateDisassemblers(IEnumerable<Molecule> reagents)
        {
            var reagentsList = reagents.ToList();
            if (reagentsList.Count == 1)
            {
                var transform = new Transform2D(OuterUnbonderPosition.Position, HexRotation.R0).RotateAbout(OuterUnbonderPosition.Position - new Vector2(ArmArea.ArmLength, 0), -HexRotation.R60);
                AddDisassembler(reagentsList[0], transform, HexRotation.R180, 0);
                return;
            }
            else if (reagentsList.Count == 2)
            {
                // If there's a single-atom reagent, add that last
                if (reagentsList[0].Atoms.Count() == 1)
                {
                    reagentsList.Reverse();
                }

                var transform = new Transform2D(OuterUnbonderPosition.Position, HexRotation.R0).RotateAbout(OuterUnbonderPosition.Position - new Vector2(ArmArea.ArmLength, 0), -HexRotation.R60);
                AddDisassembler(reagentsList[0], transform, HexRotation.R0, 0);

                transform.Position.X -= 1;
                AddDisassembler(reagentsList[1], transform, HexRotation.R0, 1);

                if (reagentsList[1].Atoms.Count() > 1)
                {
                    m_input2GrabPosition = transform;
                    m_input2GrabPosition.Position.X -= 1;
                    m_accessPoints.Add(m_input2GrabPosition);
                }
            }
        }

        private void AddDisassembler(Molecule reagent, Transform2D transform, HexRotation moleculeRotation, int index)
        {
            var disassembler = new SimpleDisassembler(this, Writer, ArmArea, transform, reagent, new Transform2D(new(), moleculeRotation));
            m_disassemblers[reagent.ID] = new DisassemblerInfo(disassembler, index);
        }

        public override void BeginSolution()
        {
            foreach (var disassembler in m_disassemblers.Values)
            {
                disassembler.Disassembler.BeginSolution();
            }
        }

        public override void Generate(Element element, int id)
        {
            var disassemblerInfo = m_disassemblers[id];
            var disassembler = disassemblerInfo.Disassembler;

            if (disassembler.Molecule.Atoms.Count() == 1)
            {
                // We don't need to unbond monoatomic molecules
                disassembler.GrabMolecule();
                return;
            }

            if (m_stashedAtom != null && m_stashedAtom.MoleculeID == id)
            {
                // Use the stashed atom before dissassembling another molecule
                ArmController.MoveGrabberTo(m_stashedAtom.Transform, this);
                ArmController.GrabAtoms(new AtomCollection(m_stashedAtom.Element, m_stashedAtom.Transform, this));
                m_stashedAtom = null;
                return;
            }

            if (m_unbondedAtom != null && m_unbondedAtom.MoleculeID != id)
            {
                if (m_stashedAtom != null)
                {
                    throw new SolverException("Cannot stash an atom when one is already stashed.");
                }

                // Stash the atom temporarily so that we can disassemble another molecule
                ArmController.MoveGrabberTo(m_unbondedAtom.Transform, this);
                ArmController.GrabAtoms(new AtomCollection(m_unbondedAtom.Element, m_unbondedAtom.Transform, this));
                ArmController.MoveGrabberTo(m_stashPosition, this);
                ArmController.DropAtoms();

                m_stashedAtom = m_unbondedAtom;
                m_stashedAtom.Transform = m_stashPosition;
                m_unbondedAtom = null;
            }

            if (m_unbondedAtom == null)
            {
                disassembler.GrabMolecule();

                var targetGrabberPosition = disassembler.MoleculeTransform.Rotation == HexRotation.R180 ? OuterUnbonderPosition : InnerUnbonderPosition;
                var otherAtomPosition = disassembler.MoleculeTransform.Rotation == HexRotation.R180 ? InnerUnbonderPosition : OuterUnbonderPosition;

                if (disassemblerInfo.Index == 1)
                {
                    // TODO: Make ArmController understand how to pivot molecules automatically so that we can simplify this
                    ArmController.PivotClockwise();
                    ArmController.PivotClockwise();
                    var tempAtoms = ArmController.DropAtoms();
                    ArmController.MoveGrabberTo(m_input2GrabPosition, this);
                    ArmController.GrabAtoms(tempAtoms);

                    var transform = InnerUnbonderPosition;
                    transform.Position.X -= 1;
                    ArmController.MoveGrabberTo(transform, this);

                    // Because we don't currently handle input suppression automatically, we need to manually
                    // re-register the input atoms after we've dropped and then picked up atoms on top of the input.
                    disassembler.RegisterInputAtoms();

                    if (ArmController.GrabbedAtoms.GetAtomAtTransformedPosition(ArmController.GetGrabberPosition()).Element == element)
                    {
                        ArmController.PivotClockwise();
                    }
                    else
                    {
                        ArmController.PivotCounterClockwise();
                        ArmController.PivotCounterClockwise();
                        targetGrabberPosition = OuterUnbonderPosition;
                        otherAtomPosition = InnerUnbonderPosition;
                    }
                }

                // Move the molecule onto the unbonder
                ArmController.MoveGrabberTo(targetGrabberPosition, this);

                m_unbondedAtom = new StoredAtom { MoleculeID = id };

                var atoms = ArmController.GrabbedAtoms;
                var grabbedAtom = atoms.GetAtomAtTransformedPosition(targetGrabberPosition.Position, this);
                if (grabbedAtom.Element == element)
                {
                    // Keep hold of the atom we've currently got
                    var otherAtom = atoms.GetAtomAtTransformedPosition(otherAtomPosition.Position, this);
                    atoms.RemoveAtom(otherAtom);
                    m_unbondedAtom.Element = otherAtom.Element;
                    m_unbondedAtom.Transform = otherAtomPosition;
                    GridState.RegisterAtom(otherAtomPosition.Position, m_unbondedAtom.Element, this);
                }
                else
                {
                    // Drop the atom we're currently holding and pick up the other atom instead
                    ArmController.DropAtoms();
                    m_unbondedAtom.Element = grabbedAtom.Element;
                    m_unbondedAtom.Transform = targetGrabberPosition;
                    ArmController.MoveGrabberTo(otherAtomPosition, this);
                    ArmController.GrabAtoms(new AtomCollection(element, otherAtomPosition, this));
                }
            }
            else
            {
                // Grab the already unbonded atom
                ArmController.MoveGrabberTo(m_unbondedAtom.Transform, this);
                ArmController.GrabAtoms(new AtomCollection(m_unbondedAtom.Element, m_unbondedAtom.Transform, this));
                m_unbondedAtom = null;
            }
        }
    }
}
