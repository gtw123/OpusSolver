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

        private List<PendingElement> m_pendingElements = new();

        public bool HasPendingElement(Element element) => m_pendingElements.Any(e => e.Element == element);

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
                throw new SolverException("possibleElements must contain at least one item.");
            }

            // Check if we can generate the element ourselves
            var elementsToGenerate = possibleElements.Where(e => CanGenerateElement(e));

            // Deal with pending elements first
            if (m_pendingElements.Any())
            {
                if (!Plan.UsePendingElementsInOrder)
                {
                    int index = m_pendingElements.FindIndex(e => possibleElements.Contains(e.Element));
                    if (index >= 0)
                    {
                        var pendingElement = m_pendingElements[index];
                        m_pendingElements.RemoveAt(index);
                        CommandSequence.Add(CommandType.Generate, pendingElement.Element, this, pendingElement.ID);
                        return pendingElement.Element;
                    }
                }

                if (Plan.UsePendingElementsInOrder || elementsToGenerate.Any())
                {
                    var pendingElement = m_pendingElements[0];
                    m_pendingElements.RemoveAt(0);
                    CommandSequence.Add(CommandType.Generate, pendingElement.Element, this, pendingElement.ID);
                    return pendingElement.Element;
                }
            }

            // Generate the element ourselves
            if (elementsToGenerate.Any())
            {
                return GenerateElement(elementsToGenerate);
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
            m_pendingElements.Add(new PendingElement { Element = element, ID = id });
        }

        /// <summary>
        /// Performs optional clean up once the command sequence has been created.
        /// </summary>
        public virtual void EndSolution()
        {
        }
    }
}
