using OpusSolver.Solver.ElementGenerators;
using System;
using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Temporarily stores a single atom that isn't currently needed.
    /// </summary>
    public class AtomBufferNoWasteArmless : LowCostAtomGenerator
    {
        private SingleStackElementBuffer.BufferInfo m_bufferInfo;

        private SingleStackElementBuffer.BufferedElement m_storedAtom;

        private static readonly Transform2D GrabPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        public override IEnumerable<Transform2D> RequiredAccessPoints => [GrabPosition];

        public AtomBufferNoWasteArmless(ProgramWriter writer, ArmArea armArea, SingleStackElementBuffer.BufferInfo bufferInfo)
            : base(writer, armArea)
        {
            m_bufferInfo = bufferInfo;
        }

        public override void Consume(Element element, int id)
        {
            var elementToStore = m_bufferInfo.Elements[id];
            if (elementToStore.RestoreOrder == null)
            {
                throw new InvalidOperationException($"{nameof(AtomBufferNoWasteArmless)} requires all atoms to be restored.");
            }

            if (m_storedAtom != null)
            {
                throw new SolverException($"{nameof(AtomBufferNoWaste)} can't store more than 1 atom.");
            }

            ArmController.DropMoleculeAt(GrabPosition, this, addToGrid: false);
            m_storedAtom = elementToStore;
        }

        public override void Generate(Element element, int id)
        {
            ArmController.SetMoleculeToGrab(new AtomCollection(element, GrabPosition, this));
        }
    }
}
