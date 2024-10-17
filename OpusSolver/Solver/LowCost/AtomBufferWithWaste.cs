using OpusSolver.Solver.ElementGenerators;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Stores atoms that are waste or aren't currently needed.
    /// </summary>
    public class AtomBufferWithWaste : LowCostAtomGenerator
    {
        private SingleStackElementBuffer.BufferInfo m_bufferInfo;
        private Arm m_arm;

        private List<SingleStackElementBuffer.BufferedElement> m_storedElements = [];

        private static readonly Transform2D GrabPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [GrabPosition];

        public AtomBufferWithWaste(ProgramWriter writer, ArmArea armArea, SingleStackElementBuffer.BufferInfo bufferInfo)
            : base(writer, armArea)
        {
            m_bufferInfo = bufferInfo;

            m_arm = new Arm(this, new(1, 0), HexRotation.R180, ArmType.Arm1, extension: 1);

            if (bufferInfo.UsesRestore && (bufferInfo.MultiAtom || bufferInfo.WastesAtoms))
            {
                // It would be a bit more efficient to have the unbonder closer to the bonder but then we risk
                // the atom chain colliding with glyphs that are clockwise from this atom buffer, or with the
                // product being assembled.
                new Glyph(this, new(2, -1), HexRotation.R180, GlyphType.Unbonding);
            }

            if (bufferInfo.MultiAtom || bufferInfo.WastesAtoms)
            {
                new Glyph(this, new(1, 1), HexRotation.R300, GlyphType.Bonding);
            }
        }

        public override void BeginSolution()
        {
            // Register dummy atoms where the atom chain will be so the solver will know to avoid them.
            for (int i = 1; i <= 6; i++)
            {
                GridState.RegisterAtom(new(i, 1), Element.Salt, this);
            }
        }

        public override void Consume(Element element, int id)
        {
            var elementToStore = m_bufferInfo.Elements[id];
            var options = new ArmMovementOptions { AllowCalcification = elementToStore.IsWaste, AllowDuplication = elementToStore.IsWaste };
            ArmController.DropMoleculeAt(GrabPosition, this, addToGrid: false, options: options);

            // If necessary, move the atom further down the atom chain so that all the atoms that need to be restored
            // before it come after it.
            var elementsToReorder = m_storedElements.Where(s => s.RestoreOrder.HasValue && (!elementToStore.RestoreOrder.HasValue || elementToStore.RestoreOrder > s.RestoreOrder)).ToList();
            if (elementsToReorder.Any())
            {
                const int MaxOutOfOrderAtoms = 2;
                if (elementsToReorder.Count > MaxOutOfOrderAtoms)
                {
                    throw new SolverException($"{nameof(AtomBufferWithWaste)} can't handle more than {MaxOutOfOrderAtoms} out-of-order atoms (solution requires {elementsToReorder.Count}).");
                }

                if (elementsToReorder.Count == 2)
                {
                    // These instructions will reverse the order of the first 3 elements in the chain (including the new one).
                    // e.g. If the chain contains ABCD and we're adding Z, this will end up with BAZCD. Later on we swap the
                    // first two elements to end up with ABZCD.

                    // Move the chain onto the unbonder to unbonded the first atom
                    Writer.Write(m_arm, [
                        Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.RotateClockwise,
                        Instruction.Grab,
                        Instruction.PivotCounterclockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise,
                        Instruction.Drop
                    ]);

                    // Shuffle the unbonded atom and the new one clockwise by one
                    Writer.Write(m_arm, [Instruction.RotateClockwise, Instruction.Grab, Instruction.RotateClockwise, Instruction.Drop, Instruction.RotateCounterclockwise]);
                    Writer.Write(m_arm, [Instruction.RotateCounterclockwise, Instruction.Grab, Instruction.RotateClockwise, Instruction.Drop, Instruction.RotateCounterclockwise]);

                    // Move the remaining chain onto the unbonder to unbonded another atom
                    Writer.Write(m_arm, [
                        Instruction.RotateCounterclockwise,
                        Instruction.Grab,
                        Instruction.RotateClockwise, Instruction.PivotCounterclockwise,
                        Instruction.Drop
                    ]);

                    // Move the rest of the chain back onto the bonder
                    Writer.Write(m_arm, [
                        Instruction.RotateCounterclockwise,
                        Instruction.Grab,
                        Instruction.PivotClockwise, Instruction.RotateCounterclockwise, Instruction.PivotClockwise,
                        Instruction.Drop
                    ]);

                    // Bond the new atom to the front of the chain
                    Writer.Write(m_arm, [
                        Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise,
                        Instruction.Grab,
                        Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise,
                        Instruction.Drop
                    ]);

                    // Bond the first unbonded atom to the front of the chain
                    Writer.Write(m_arm, [
                        Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise,
                        Instruction.Grab,
                        Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise,
                        Instruction.Drop
                    ]);

                    // Move the second unbonded atom to the grab position
                    Writer.Write(m_arm, [
                        Instruction.RotateClockwise, Instruction.RotateClockwise,
                        Instruction.Grab,
                        Instruction.RotateClockwise,
                        Instruction.Drop
                    ]);
                }

                // These instructions will reverse the order of the first 2 elements in the chain (including the new one).
                // e.g. If the chain contains ABCD and we're adding Z, this will end up with AZBCD

                // Move the chain onto the unbonder to unbonded the first atom
                Writer.Write(m_arm, [
                    Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.RotateClockwise,
                    Instruction.Grab,
                    Instruction.PivotCounterclockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise,
                    Instruction.Drop
                ]);

                // Move the rest of the chain back onto the bonder
                Writer.Write(m_arm, [
                    Instruction.RotateCounterclockwise,
                    Instruction.Grab,
                    Instruction.PivotClockwise, Instruction.RotateCounterclockwise, Instruction.PivotClockwise,
                    Instruction.Drop
                ]);

                // Bond the new atom to the front of the chain
                Writer.Write(m_arm, [
                    Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise,
                    Instruction.Grab,
                    Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise,
                    Instruction.Drop
                ]);

                // Bond the previously unbonded atom to the front of the chain
                Writer.Write(m_arm, [
                    Instruction.RotateClockwise, Instruction.RotateClockwise,
                    Instruction.Grab,
                    Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise,
                    Instruction.Reset
                ]);

                var insertBefore = elementsToReorder.First();
                int insertPosition = m_storedElements.IndexOf(insertBefore);
                m_storedElements.Insert(insertPosition, elementToStore);
            }
            else
            {
                // Bond the atom to the waste chain
                Writer.AdjustTime(-1);
                Writer.WriteGrabResetAction(m_arm, [Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise]);

                m_storedElements.Add(elementToStore);
            }
        }

        public override void Generate(Element element, int id)
        {
            ArmController.MoveGrabberTo(GrabPosition, this);

            // Create a new fragment so that the drop instructions for the buffer arm will automatically line up with
            // the grab for the main arm if possible.
            Writer.NewFragment();

            // TODO: Doesn't work with NewFragment (collision)
            // Does work without it
            // Does work if we manually remove the gaps in the program

            var restoredElement = m_storedElements.Last();
            if (restoredElement.Index != id)
            {
                throw new SolverException($"Trying to restore atom {id} but atom {restoredElement.Index} is currently at the start of the queue.");
            }

            // Move the chain onto the unbonder
            Writer.Write(m_arm, [
                Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.RotateClockwise,
                Instruction.Grab,
                Instruction.PivotCounterclockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise
            ]);

            // Move the unbonded atom to the grab position
            Writer.Write(m_arm, [Instruction.RotateClockwise, Instruction.Drop]);

            // Move the remaining chain back onto the bonder
            Writer.Write(m_arm, [
                Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise,
                Instruction.Grab,
                Instruction.PivotClockwise, Instruction.RotateCounterclockwise, Instruction.PivotClockwise,
                Instruction.Reset
            ], updateTime: false);

            Writer.AdjustTime(-1);
            ArmController.GrabMolecule(new AtomCollection(element, GrabPosition, this));

            m_storedElements.Remove(restoredElement);
        }
    }
}
