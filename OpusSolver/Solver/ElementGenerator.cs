using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver
{
    /// <summary>
    /// Generates elements from other elements, building up a command sequence that can
    /// ultimately be used to generate a program.
    /// </summary>
    public abstract class ElementGenerator
    {
        public ElementGenerator Parent { get; set; }
        public AtomGenerator AtomGenerator { get; set; }

        public bool HasPendingElements => m_pendingElements.Any();

        public SolutionPlan Plan { get; private set; }
        public Recipe Recipe => Plan.Recipe;
        public int CurrentUsages { get; private set; } = 0;

        protected CommandSequence CommandSequence { get; private set; }

        private class PendingElement
        {
            public Element Element;
            public int ID;
        }

        private Queue<PendingElement> m_pendingElements = new Queue<PendingElement>();

        protected ElementGenerator(CommandSequence commandSequence, SolutionPlan plan)
        {
            CommandSequence = commandSequence;
            Plan = plan;
        }

        /// <summary>
        /// Generates a specified element. If this element generator cannot generate it,
        /// it will recursively try to generate it from its parent element generator.
        /// </summary>
        public Element RequestElement(Element element)
        {
            return RequestElement([element]);
        }

        /// <summary>
        /// Generates one of the specified elements. If this element generator cannot generate it,
        /// it will recursively try to generate it from its parent element generator.
        /// </summary>
        public Element RequestElement(IEnumerable<Element> possibleElements)
        {
            if (possibleElements.Count() == 0)
            {
                throw new ArgumentException("possibleElements must contain at least one item.", "possibleElements");
            }

            // Generate the element ourselves if we can (but only if no pending elements are in the way)
            var elementsToGenerate = possibleElements.Where(e => CanGenerateElement(e));
            if (elementsToGenerate.Any() && !m_pendingElements.Any())
            {
                return GenerateElement(elementsToGenerate);
            }

            if (m_pendingElements.Any())
            {
                // Use the pending elements if there's a matching one or if we're not allowed to pass through when
                // there are pending elements
                if (!Plan.AllowPassthroughWithPendingElements || possibleElements.Contains(m_pendingElements.Peek().Element))
                {
                    var pendingElement = m_pendingElements.Dequeue();
                    CommandSequence.Add(CommandType.Generate, pendingElement.Element, this, pendingElement.ID);
                    return pendingElement.Element;
                }
            }

            // If we couldn't generate it, try our parent
            if (Parent != null)
            {
                var generated = Parent.RequestElement(possibleElements);
                while (!possibleElements.Contains(generated))
                {
                    StoreElement(generated);
                    generated = Parent.RequestElement(possibleElements);
                }

                PassThrough(generated);
                return generated;
            }

            throw new SolverException(Invariant($"Cannot find suitable generator to generate {String.Join(", ", possibleElements)}."));
        }

        protected abstract bool CanGenerateElement(Element element);

        /// <summary>
        /// Adds the commands required to generate one of the specified elements.
        /// </summary>
        protected abstract Element GenerateElement(IEnumerable<Element> possibleElements);

        /// <summary>
        /// Adds the commands required to store the specified element so that it doesn't get
        /// in the way of future elements.
        /// </summary>
        protected virtual void StoreElement(Element element)
        {
            throw new SolverException(Invariant($"{GetType()} cannot store elements. Requested to store: {element}."));
        }

        /// <summary>
        /// Adds the commands required to pass an element through this generator without modifying it.
        /// </summary>
        protected void PassThrough(Element element)
        {
            CommandSequence.Add(CommandType.PassThrough, element, this);
        }

        protected void AddPendingElement(Element element, int id = 0)
        {
            m_pendingElements.Enqueue(new PendingElement { Element = element, ID = id });
        }

        /// <summary>
        /// Performs optional clean up once the command sequence has been created.
        /// </summary>
        public virtual void EndSolution()
        {
        }
    }
}
