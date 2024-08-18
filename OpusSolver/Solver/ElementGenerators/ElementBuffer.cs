using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Temporarily stores elements that aren't needed by the rest of the pipeline yet.
    /// </summary>
    public class ElementBuffer : ElementGenerator
    {
        /// <summary>
        /// Specifies various properties of a stack; used for optimising the components needed by the stack.
        /// </summary>
        public class StackInfo
        {
            /// <summary>
            /// Indicates whether the stack ever needs to store more than one atom at atime.
            /// </summary>
            public bool MultiAtom { get; set; }

            /// <summary>
            /// Indicates whether atoms are ever restored from the stack.
            /// </summary>
            public bool UsesRestore { get; set; }

            /// <summary>
            /// Indicates whether the stack has leftover atoms at the end of the solution.
            /// </summary>
            public bool WastesAtoms { get; set; }
        }

        private class ElementStack
        {
            public int ID;
            public Stack<Element> Elements = new Stack<Element>();
            public int MaxCount;
            public bool UsedPop;
        }

        private List<ElementStack> m_stacks = new List<ElementStack>();

        public IEnumerable<StackInfo> StackInfos => m_stacks.Select(stack => new StackInfo {
            MultiAtom = stack.MaxCount > 1,
            UsesRestore = stack.UsedPop,
            WastesAtoms = stack.Elements.Count > 0
        });

        public ElementBuffer(CommandSequence commandSequence, SolutionPlan plan)
            : base(commandSequence, plan)
        {
        }

        protected override bool CanGenerateElement(Element element) => m_stacks.Any(stack => stack.Elements.Contains(element));

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
    }
}
