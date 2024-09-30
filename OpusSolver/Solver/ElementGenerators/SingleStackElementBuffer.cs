using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Temporarily stores elements that aren't needed by the rest of the pipeline yet.
    /// Elements are stored in a single stack but can be restored in any order.
    /// </summary>
    public class SingleStackElementBuffer : ElementBuffer
    {
        public class BufferInfo
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

        public class BufferedElement(Element element, int index)
        {
            public Element Element { get; init; } = element;
            public int Index { get; init; } = index;
            public bool IsStored { get; set; } = true;
        }

        public List<BufferedElement> m_elements = new();
        private int m_maxStoredCount;
  
        public SingleStackElementBuffer(CommandSequence commandSequence, SolutionPlan plan)
            : base(commandSequence, plan)
        {
        }

        public BufferInfo GetBufferInfo()
        {
            return new BufferInfo
            {
                MultiAtom = m_maxStoredCount > 1,
                UsesRestore = m_elements.Any(e => !e.IsStored),
                WastesAtoms = m_elements.Any(e => e.IsStored),
                Elements = m_elements,
            };
        }

        protected override bool CanGenerateElement(Element element)
        {
            // Don't automatically restore elements when using this buffer - instead, element generators will
            // do it themselves.
            return false;
        }

        public override bool CanRestoreElement(Element element)
        {
            return m_elements.Any(e => e.IsStored && e.Element == element);
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            var storedElement = m_elements.LastOrDefault(e => e.IsStored && possibleElements.Contains(e.Element));
            if (storedElement != null)
            {
                storedElement.IsStored = false;

                var element = storedElement.Element;
                CommandSequence.Add(CommandType.Generate, element, this);
                return element;
            }

            throw new SolverException(Invariant($"Can't find any of {string.Join(", ", possibleElements)} in buffer."));
        }

        public override void StoreElement(Element element)
        {
            m_elements.Add(new BufferedElement(element, m_elements.Count));
            m_maxStoredCount = Math.Max(m_maxStoredCount, m_elements.Count(e => e.IsStored));

            CommandSequence.Add(CommandType.Consume, element, this);
        }
    }
}
