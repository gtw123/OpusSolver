using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// A disassembler used when all reagents have at most two atoms.
    /// </summary>
    public class DiatomicDisassembler : LowCostAtomGenerator
    {
        private record DisassemblerInfo(MoleculeInput Disassembler, int Index);

        private readonly Dictionary<int, DisassemblerInfo> m_disassemblers = new();

        private class StoredAtom
        {
            public AtomCollection Atoms;
            public int MoleculeID;
        }

        private StoredAtom m_unbondedAtom;
        private StoredAtom m_stashedAtom;

        public const int MaxReagents = 3;

        private readonly bool m_reverseElementOrder;

        private readonly int m_requiredWidth;
        public override int RequiredWidth => m_requiredWidth;

        private static readonly Transform2D InnerUnbonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D OuterUnbonderPosition = new Transform2D(new Vector2(1, 0), HexRotation.R0);
        private readonly Transform2D m_stashPosition;

        private readonly List<Transform2D> m_accessPoints = new();
        public override IEnumerable<Transform2D> RequiredAccessPoints => m_accessPoints;

        public DiatomicDisassembler(ProgramWriter writer, ArmArea armArea, IEnumerable<Molecule> reagents, bool reverseElementOrder, bool addExtraWidth)
            : base(writer, armArea)
        {
            if (reagents.Any(r => r.Atoms.Count() > 2))
            {
                throw new ArgumentException($"{nameof(DiatomicDisassembler)} can't handle reagents with more than two atoms.");
            }

            if (reagents.Count() > MaxReagents)
            {
                throw new ArgumentException(Invariant($"{nameof(DiatomicDisassembler)} can't handle more than {MaxReagents} distinct reagents."));
            }

            m_reverseElementOrder = reverseElementOrder;
            m_requiredWidth = addExtraWidth ? 2 : 1;

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
            else if (reagentsList.Count >= 2)
            {
                // Sort the reagents so we add single-atom reagents last
                reagentsList = reagents.OrderByDescending(r => r.Atoms.Count()).ToList();

                var transform = new Transform2D(OuterUnbonderPosition.Position, HexRotation.R0).RotateAbout(OuterUnbonderPosition.Position - new Vector2(ArmArea.ArmLength, 0), -HexRotation.R60);
                AddDisassembler(reagentsList[0], transform, HexRotation.R0, 0);

                transform.Position.X -= 1;
                AddDisassembler(reagentsList[1], transform, HexRotation.R0, 1);

                if (reagentsList[1].Atoms.Count() > 1)
                {
                    var grabPosition = transform;
                    grabPosition.Position.X -= 1;
                    m_accessPoints.Add(grabPosition);
                }

                if (reagentsList.Count >= 3)
                {
                    if (reagentsList[2].Atoms.Count() == 1)
                    {
                        var reagentTransform = new Transform2D(transform.Position += new Vector2(1, -1), transform.Rotation);
                        AddDisassembler(reagentsList[2], reagentTransform, HexRotation.R0, 2);
                        m_accessPoints.Add(reagentTransform);
                    }
                    else
                    {
                        // TODO: Find a better location for this molecule
                        var reagentTransform = new Transform2D(transform.Position += new Vector2(-1, 1), HexRotation.R0);
                        AddDisassembler(reagentsList[2], reagentTransform, HexRotation.R0, 2);
                        m_accessPoints.Add(reagentTransform);
                    }
                }
            }
        }

        private void AddDisassembler(Molecule reagent, Transform2D transform, HexRotation moleculeRotation, int index)
        {
            var disassembler = new MoleculeInput(this, Writer, ArmArea, transform, reagent, new Transform2D(new(), moleculeRotation));
            m_disassemblers[reagent.ID] = new DisassemblerInfo(disassembler, index);
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
                var atomPos = new Vector2(0, 0);
                var requiredRotation = HexRotation.R0;
                if (disassembler.GetAtomAtPosition(atomPos).Element != element)
                {
                    atomPos = new Vector2(1, 0);
                    requiredRotation = HexRotation.R180;
                }

                if (m_reverseElementOrder)
                {
                    atomPos.X = 1 - atomPos.X;
                    requiredRotation = HexRotation.R180 - requiredRotation;
                }

                var targetTransform = new Transform2D(InnerUnbonderPosition.Position - atomPos, HexRotation.R0);
                targetTransform = targetTransform.RotateAbout(InnerUnbonderPosition.Position, requiredRotation);

                var molecule = disassembler.GrabMolecule();
                var options = new ArmMovementOptions
                {
                    AllowUnbonding = true,
                    FinalBondToRemove = (molecule.Atoms[0].Position, molecule.Atoms[1].Position)
                };
                ArmController.MoveMoleculeTo(targetTransform, this, options: options);

                var targetGrabberPosition = GetWorldTransform().Inverse().Apply(ArmController.GetGrabberPosition());
                var otherAtomPosition = targetGrabberPosition == OuterUnbonderPosition.Position ? InnerUnbonderPosition : OuterUnbonderPosition;

                m_unbondedAtom = new StoredAtom { MoleculeID = id };

                molecule = ArmController.GrabbedMolecule;
                var grabbedAtom = molecule.GetAtomAtWorldPosition(targetGrabberPosition, this);
                if (grabbedAtom.Element == element)
                {
                    // Keep hold of the atom we've currently got
                    var removedAtoms = ArmController.UnbondGrabbedAtomFromOthers();
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
