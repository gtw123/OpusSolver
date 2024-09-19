using OpusSolver.Solver.ElementGenerators;
using System;
using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Temporarily stores atoms that aren't currently needed.
    /// </summary>
    public class AtomBuffer : LowCostAtomGenerator
    {
        private ElementBuffer.BufferInfo m_bufferInfo;
        private WasteDisposer m_wasteDisposer;

        public AtomBuffer(ProgramWriter writer, ArmArea armArea, ElementBuffer.BufferInfo bufferInfo, WasteDisposer wasteDisposer)
            : base(writer, armArea)
        {
            if (bufferInfo.Stacks.Any(s => !s.Elements.All(e => e.IsWaste)))
            {
                throw new InvalidOperationException("LowCost AtomBuffer currently only supports stacks that are entirely waste.");
            }

            m_bufferInfo = bufferInfo;
            m_wasteDisposer = wasteDisposer;
        }

        public override void Consume(Element element, int id)
        {
            m_wasteDisposer.Consume(element, id);
        }

        public override void Generate(Element element, int id)
        {
            throw new InvalidOperationException("LowCost AtomBuffer doesn't currently support generating atoms.");
        }
    }
}
