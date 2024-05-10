using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace Opus.Solution.Solver.ElementGenerators
{
    /// <summary>
    /// Temporarily stores elements that aren't needed by the rest of the pipeline yet.
    /// </summary>
    public class ElementBuffer : ElementGenerator
    {
        private class ElementStack
        {
            public int ID;
            public Stack<Element> Elements = new Stack<Element>();
            public int MaxCount;
            public bool UsedPop;
        }

        private List<ElementStack> m_stacks = new List<ElementStack>();

        public ElementBuffer(CommandSequence commandSequence)
            : base(commandSequence)
        {
        }

        public override IEnumerable<Element> OutputElements => m_stacks.SelectMany(stack => stack.Elements);

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            foreach (var element in possibleElements)
            {
                var stack = m_stacks.FirstOrDefault(s => s.Elements.Any() && s.Elements.First() == element);
                if (stack != null)
                {
                    CommandSequence.Add(CommandType.Generate, element, this, stack.ID);
                    stack.UsedPop = true;
                    return stack.Elements.Pop();
                }
            }

            throw new SolverException(Invariant($"Can't find any of {String.Join(", ", possibleElements)} in buffer."));
        }

        protected override void StoreElement(Element element)
        {
            var stack = m_stacks.FirstOrDefault(s => s.Elements.FirstOrDefault() == element);
            if (stack == null)
            {
                stack = m_stacks.FirstOrDefault(s => !s.Elements.Any());
            }
            if (stack == null)
            {
                stack = new ElementStack { ID = m_stacks.Count() };
                m_stacks.Add(stack);
            }

            stack.Elements.Push(element);
            stack.MaxCount = Math.Max(stack.MaxCount, stack.Elements.Count);

            CommandSequence.Add(CommandType.Consume, element, this, stack.ID);
        }

        public override void EndSolution()
        {
            while (Parent.HasPendingElements)
            {
                StoreElement(Parent.RequestElement(PeriodicTable.AllElements));
            }
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            return new AtomGenerators.AtomBuffer(writer, m_stacks.Select(stack => new AtomGenerators.AtomBuffer.StackInfo {
                MultiAtom = stack.MaxCount > 1,
                UsesRestore = stack.UsedPop,
                WastesAtoms = stack.Elements.Count > 0
            }));
        }
    }
}
