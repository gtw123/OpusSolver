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
        private readonly Dictionary<int, MoleculeDisassembler> m_disassemblers = new();

        private Element? m_unbondedElement = null;
        private Transform2D? m_unbondedElementTransform = null;

        public const int MaxReagents = 1;

        private static readonly Transform2D InnerUnbonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D OuterUnbonderPosition = new Transform2D(new Vector2(1, 0), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [OuterUnbonderPosition, InnerUnbonderPosition];

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

            CreateDisassemblers(reagents);
        }

        private void CreateDisassemblers(IEnumerable<Molecule> reagents)
        {
            var reagentsList = reagents.ToList();
            if (reagentsList.Count == 1)
            {
                var transform = new Transform2D(InnerUnbonderPosition.Position, HexRotation.R0).RotateAbout(new Vector2(-ArmArea.ArmLength, 0), -HexRotation.R60);
                AddDisassembler(reagentsList[0], transform);
                return;
            }
        }

        private void AddDisassembler(Molecule reagent, Transform2D transform)
        {
            var disassembler = new DiatomicDisassembler(this, Writer, ArmArea, transform, reagent);
            m_disassemblers[reagent.ID] = disassembler;
        }

        public override void BeginSolution()
        {
            foreach (var disassembler in m_disassemblers.Values)
            {
                disassembler.BeginSolution();
            }
        }

        public override void Generate(Element element, int id)
        {
            var disassembler = m_disassemblers[id];

            if (m_unbondedElement == null)
            {
                // Move the molecule onto the unbonder
                disassembler.GenerateNextAtom();
                ArmArea.MoveGrabberTo(InnerUnbonderPosition, this);

                var atoms = ArmArea.GrabbedAtoms;
                var grabbedAtom = atoms.GetAtomAtTransformedPosition(InnerUnbonderPosition.Position, this);
                if (grabbedAtom.Element == element)
                {
                    var otherAtom = atoms.GetAtomAtTransformedPosition(OuterUnbonderPosition.Position, this);
                    atoms.RemoveAtom(otherAtom);
                    GridState.RegisterAtom(OuterUnbonderPosition.Position, m_unbondedElement, this);
                    m_unbondedElement = otherAtom.Element;
                    m_unbondedElementTransform = OuterUnbonderPosition;
                }
                else
                {
                    // Drop the atom we're currently holding and pick up the other atom instead
                    ArmArea.DropAtoms();
                    m_unbondedElement = grabbedAtom.Element;
                    m_unbondedElementTransform = InnerUnbonderPosition;
                    ArmArea.MoveGrabberTo(OuterUnbonderPosition, this);
                    ArmArea.GrabAtoms(new AtomCollection(element, OuterUnbonderPosition, this));
                }
            }
            else
            {
                ArmArea.MoveGrabberTo(m_unbondedElementTransform.Value, this);
                ArmArea.GrabAtoms(new AtomCollection(m_unbondedElement.Value, m_unbondedElementTransform.Value, this));
                m_unbondedElement = null;
                m_unbondedElementTransform = null;
            }
        }
    }
}
