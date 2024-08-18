using OpusSolver.Solver.ElementGenerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Temporarily stores atoms that aren't currently needed.
    /// </summary>
    public class AtomBuffer : AtomGenerator
    {
        public AtomBuffer(ProgramWriter writer, IEnumerable<ElementBuffer.StackInfo> stackInfo)
            : base(writer)
        {
            if (stackInfo.Any())
            {
                throw new InvalidOperationException("LowCost AtomBuffer doesn't currently support any stacks.");
            }
        }
    }
}
