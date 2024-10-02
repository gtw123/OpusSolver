using OpusSolver.Solver.ElementGenerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Temporarily stores atoms that aren't currently needed.
    /// </summary>
    public class AtomBufferNoWaste : LowCostAtomGenerator
    {
        private SingleStackElementBuffer.BufferInfo m_bufferInfo;
        private Arm m_arm;

        private HexRotationDictionary<SingleStackElementBuffer.BufferedElement> m_storedAtoms = new();

        private static readonly Transform2D GrabPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        private static readonly HexRotation GrabDirection = HexRotation.R180;
        private static readonly HexRotation NextAtomDirection = HexRotation.R240;

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [GrabPosition];

        public AtomBufferNoWaste(ProgramWriter writer, ArmArea armArea, SingleStackElementBuffer.BufferInfo bufferInfo)
            : base(writer, armArea)
        {
            m_bufferInfo = bufferInfo;

            m_arm = new Arm(this, new(2, 0), HexRotation.R180, ArmType.Arm1, extension: 2);
        }

        public override void BeginSolution()
        {
            // Register dummy atoms where atoms will be stored so the solver will know to avoid them.
            // TODO: Do this more accurately
            foreach (var dir in HexRotation.All)
            {
                if (dir != GrabDirection)
                {
                    var pos = m_arm.Transform.Position + new Vector2(1, 0).RotateBy(dir);
                    GridState.RegisterAtom(pos, Element.Salt, this);
                }
            }
        }

        public override void Consume(Element element, int id)
        {
            var elementToStore = m_bufferInfo.Elements[id];
            if (elementToStore.RestoreOrder == null)
            {
                throw new InvalidOperationException($"{nameof(AtomBufferNoWaste)} requires all atoms to be restored.");
            }

            if (m_storedAtoms.Count == 5)
            {
                // TODO: Use AtomBufferWithWaste in this case, as long as we don't need to reorder too many atoms
                throw new SolverException($"{nameof(AtomBufferNoWaste)} can't store more than 5 atoms.");
            }

            // Find any stored atoms that need to be restored before this one
            var elementsToReorder = m_storedAtoms.Values.Where(s => s.RestoreOrder < elementToStore.RestoreOrder).ToList();
            if (elementsToReorder.Any() && elementsToReorder.Count == m_storedAtoms.Count)
            {
                // As an optimization we can just rotate this atom clockwise to put it at the end of the queue
                ArmArea.MoveGrabberTo(GrabPosition, this);
                ArmArea.DropAtoms(addToGrid: false);

                Writer.Write(m_arm, Instruction.Grab);
                var targetDir = m_storedAtoms.EnumerateCounterclockwise(startFrom: NextAtomDirection).Last().Key + HexRotation.R60;
                Writer.Write(m_arm, GrabDirection.CalculateClockwiseDeltaRotationsTo(targetDir).Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise));
                Writer.Write(m_arm, Instruction.Reset);

                m_storedAtoms[targetDir] = elementToStore;

                return;
            }

            if (elementsToReorder.Any())
            {
                throw new SolverException($"{nameof(AtomBufferNoWaste)} doesn't currently support restoring atoms out of order.");
            }

            // Shuffle the existing atoms counterclockwise so we can add the new atom in
            var currentArmRot = GrabDirection;

            if (m_storedAtoms.Any())
            {
                Writer.NewFragment();
                var targetDir = m_storedAtoms.EnumerateCounterclockwise(startFrom: NextAtomDirection).Last().Key + HexRotation.R60;
                foreach (var (dir, atom) in m_storedAtoms.EnumerateClockwise(startFrom: GrabDirection).ToList())
                {
                    Writer.Write(m_arm, currentArmRot.CalculateDeltaRotationsTo(dir).Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise));
                    Writer.Write(m_arm, Instruction.Grab);
                    Writer.Write(m_arm, dir.CalculateCounterclockwiseDeltaRotationsTo(targetDir).Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise));
                    Writer.Write(m_arm, Instruction.Drop);

                    m_storedAtoms[targetDir] = atom;
                    m_storedAtoms.Remove(dir);

                    currentArmRot = targetDir;
                    targetDir = targetDir.Rotate60Clockwise();
                }
            }

            ArmArea.MoveGrabberTo(GrabPosition, this);
            ArmArea.DropAtoms(addToGrid: false);

            Writer.Write(m_arm, currentArmRot.CalculateDeltaRotationsTo(GrabDirection).Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise));
            Writer.Write(m_arm, Instruction.Grab);
            Writer.Write(m_arm, GrabDirection.CalculateCounterclockwiseDeltaRotationsTo(NextAtomDirection).Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise));
            Writer.Write(m_arm, Instruction.Reset);

            m_storedAtoms[NextAtomDirection] = elementToStore;
        }

        public override void Generate(Element element, int id)
        {
            ArmArea.MoveGrabberTo(GrabPosition, this);

            // Create a new fragment so that the drop instructions for the buffer arm will automatically line up with
            // the grab for the main arm if possible.
            Writer.NewFragment();

            Writer.Write(m_arm, [Instruction.RotateCounterclockwise, Instruction.Grab, Instruction.RotateClockwise, Instruction.Drop]);
            m_storedAtoms.Remove(NextAtomDirection);

            Writer.AdjustTime(-1);
            ArmArea.GrabAtoms(new AtomCollection(element, GrabPosition, this));

            Writer.NewFragment();

            // Shuffle the remaining atoms around so that the next one is ready to be restored
            var currentDir = GrabDirection;
            var targetDir = NextAtomDirection;
            foreach (var (dir, atom) in m_storedAtoms.EnumerateCounterclockwise(startFrom: NextAtomDirection).ToList())
            {
                Writer.Write(m_arm, currentDir.CalculateDeltaRotationsTo(dir).Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise));
                Writer.Write(m_arm, Instruction.Grab);
                Writer.Write(m_arm, dir.CalculateClockwiseDeltaRotationsTo(targetDir).Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise));
                Writer.Write(m_arm, Instruction.Drop);

                m_storedAtoms[targetDir] = atom;
                m_storedAtoms.Remove(dir);

                currentDir = targetDir;
                targetDir = targetDir.Rotate60Counterclockwise();
            }

            Writer.Write(m_arm, Instruction.Reset);
        }
    }
}
