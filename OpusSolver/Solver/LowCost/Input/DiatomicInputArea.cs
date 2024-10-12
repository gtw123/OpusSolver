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
            public AtomCollection Atoms;
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
                ArmController.SetMoleculeToGrab(m_stashedAtom.Atoms);
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
                ArmController.SetMoleculeToGrab(m_unbondedAtom.Atoms);
                ArmController.DropMoleculeAt(m_stashPosition, this);

                m_stashedAtom = m_unbondedAtom;
                m_unbondedAtom = null;
            }

            if (m_unbondedAtom == null)
            {
                Transform2D targetTransform = new Transform2D(InnerUnbonderPosition.Position, HexRotation.R0);
                if (disassemblerInfo.Index == 0)
                {
                    if (disassembler.MoleculeTransform.Rotation == HexRotation.R180)
                    {
                        targetTransform = new Transform2D(OuterUnbonderPosition.Position, HexRotation.R180);
                    }
                }
                else
                {
                    if (disassembler.GetAtomAtPosition(new Vector2(0, 0)).Element != element)
                    {
                        targetTransform = new Transform2D(OuterUnbonderPosition.Position, HexRotation.R180);
                    }
                }

                disassembler.GrabMolecule();
                ArmController.MoveMoleculeTo(targetTransform, this);

                var targetGrabberPosition = GetWorldTransform().Inverse().Apply(ArmController.GetGrabberPosition());
                var otherAtomPosition = targetGrabberPosition == OuterUnbonderPosition.Position ? InnerUnbonderPosition : OuterUnbonderPosition;

                m_unbondedAtom = new StoredAtom { MoleculeID = id };

                var molecule = ArmController.GrabbedMolecule;
                var grabbedAtom = molecule.GetAtomAtWorldPosition(targetGrabberPosition, this);
                if (grabbedAtom.Element == element)
                {
                    // Keep hold of the atom we've currently got
                    var removedAtoms = ArmController.RemoveAllExceptGrabbedAtom();
                    m_unbondedAtom.Atoms = new AtomCollection(removedAtoms.Atoms.Single().Element, otherAtomPosition, this);
                    GridState.RegisterMolecule(m_unbondedAtom.Atoms);
                }
                else
                {
                    // TODO: Simplify this to use RemoveAllExceptGrabbedAtom

                    // Drop the atom we're currently holding and pick up the other atom instead
                    ArmController.DropMolecule();

                    m_unbondedAtom.Atoms = new AtomCollection(grabbedAtom.Element, new Transform2D(targetGrabberPosition, HexRotation.R0), this);
                    ArmController.SetMoleculeToGrab(new AtomCollection(element, otherAtomPosition, this));
                }
            }
            else
            {
                // Grab the already unbonded atom
                ArmController.SetMoleculeToGrab(m_unbondedAtom.Atoms);
                m_unbondedAtom = null;
            }
        }
    }
}
