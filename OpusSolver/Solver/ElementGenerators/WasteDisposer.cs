using System;
using System.Collections.Generic;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Disposes of waste atoms.
    /// </summary>
    public class WasteDisposer : ElementGenerator
    {
        public WasteDisposer(CommandSequence commandSequence, SolutionPlan plan)
            : base(commandSequence, plan)
        {
        }

        protected override bool CanGenerateElement(Element element) => false;

        // Ideally we'd implement Consume here to consume waste atoms but that's tricky to do at the moment
        // because we don't know which atoms will be waste until all commands have been generated.

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            throw new InvalidOperationException("Can't call GenerateElement on a WasteDisposer.");
        }
    }
}
