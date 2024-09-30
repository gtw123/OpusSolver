using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Temporarily stores elements that aren't needed by the rest of the pipeline yet.
    /// Elements are stored in stacks of a single element.
    /// </summary>
    public class MultiStackElementBuffer : ElementBuffer
    {
        public record class BufferInfo(IReadOnlyList<StackInfo> Stacks);

        public class BufferedElement(Element element)
        {
            public Element Element { get; init; } = element;
            public bool IsWaste { get; set; } = true;
        }

        /// <summary>
        /// Specifies various properties of a stack; used for optimising the components needed by the stack.
        /// </summary>
        public class StackInfo
        {
            /// <summary>
            /// Indicates whether the stack ever needs to store more than one atom at a time.
            /// </summary>
            public bool MultiAtom { get; init; }

            /// <summary>
            /// Indicates whether atoms are ever restored from the stack.
            /// </summary>
            public bool UsesRestore { get; init; }

            /// <summary>
            /// Indicates whether the stack has atoms that are never used in the solution.
            /// </summary>
            public bool WastesAtoms { get; init; }

            /// <summary>
            /// All the elements that are added to this stack, in order.
            /// </summary>
            public IReadOnlyList<BufferedElement> Elements { get; init; }
        }

        private class ElementStack
        {
            public int ID;
            public Stack<Element> CurrentElements = new();
            public List<BufferedElement> AllElements = new();
            public int MaxCount;
            public bool UsedPop;
            public bool HasWaste => CurrentElements.Count > 0;
        }

        private List<ElementStack> m_stacks = new();

        public MultiStackElementBuffer(CommandSequence commandSequence, SolutionPlan plan)
            : base(commandSequence, plan)
        {
        }

        public BufferInfo GetBufferInfo()
        {
            var stacks = m_stacks.Select(stack => new StackInfo
            {
                MultiAtom = stack.MaxCount > 1,
                UsesRestore = stack.UsedPop,
                WastesAtoms = stack.HasWaste,
                Elements = stack.AllElements,
            }).ToList();

            return new BufferInfo(stacks);
        }

        protected override bool CanGenerateElement(Element element)
        {
            // Prefer using a pending element if there's one available rather than restoring one from the buffer.
            // This helps avoid this buffer needing to store more than one element in many cases.
            if (Parent != null && Parent.HasPendingElement(element))
            {
                return false;
            }

            return CanRestoreElement(element);
        }

        public override bool CanRestoreElement(Element element)
        {
            return m_stacks.Any(stack => stack.CurrentElements.Contains(element));
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            foreach (var element in possibleElements)
            {
                var stack = m_stacks.FirstOrDefault(s => s.CurrentElements.Any() && s.CurrentElements.First() == element);
                if (stack != null)
                {
                    CommandSequence.Add(CommandType.Generate, element, this, stack.ID);
                    stack.UsedPop = true;
                    stack.AllElements.Last().IsWaste = false;
                    return stack.CurrentElements.Pop();
                }
            }

            throw new SolverException(Invariant($"Can't find any of {String.Join(", ", possibleElements)} in buffer."));
        }

        public override void StoreElement(Element element)
        {
            var stack = m_stacks.FirstOrDefault(s => s.CurrentElements.FirstOrDefault() == element);
            if (stack == null)
            {
                stack = m_stacks.FirstOrDefault(s => !s.CurrentElements.Any());
            }
            if (stack == null)
            {
                stack = new ElementStack { ID = m_stacks.Count() };
                m_stacks.Add(stack);
            }

            stack.CurrentElements.Push(element);
            stack.AllElements.Add(new BufferedElement(element));
            stack.MaxCount = Math.Max(stack.MaxCount, stack.CurrentElements.Count);

            CommandSequence.Add(CommandType.Consume, element, this, stack.ID);
        }
    }
}
