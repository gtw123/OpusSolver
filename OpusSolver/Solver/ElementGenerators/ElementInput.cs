using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates elements from an input (reagent).
    /// </summary>
    public class ElementInput
    {
        public Molecule Molecule { get; private set; }
        public SolutionPlan Plan { get; private set; }

        public bool HasPendingElements => m_currentIndex > 0;

        private List<Element> m_originalElementSequence;
        private List<Element> m_currentElementSequence;
        private int m_currentIndex;
        private bool m_isReversible;
     
        public ElementInput(Molecule molecule, SolutionPlan plan)
        {
            Molecule = molecule;
            Plan = plan;

            var elementInfo = plan.GetReagentElementInfo(molecule);
            m_originalElementSequence = elementInfo.ElementOrder.ToList();
            m_isReversible = elementInfo.IsElementOrderReversible;
        }

        public Element GetNextElement(IEnumerable<Element> preferredElements)
        {
            if (m_currentElementSequence == null)
            {
                m_currentElementSequence = new List<Element>(m_originalElementSequence);

                if (m_isReversible && !preferredElements.Contains(m_currentElementSequence.First()) && preferredElements.Contains(m_currentElementSequence.Last()))
                {
                    m_currentElementSequence.Reverse();
                }
            }

            var element = m_currentElementSequence[m_currentIndex];

            m_currentIndex++;
            if (m_currentIndex >= m_currentElementSequence.Count)
            {
                m_currentIndex = 0;
                m_currentElementSequence = null;
                Plan.Recipe.RecordReactionUsage(ReactionType.Reagent, id: Molecule.ID);
            }

            return element;
        }

        public int? FindClosestElement(IEnumerable<Element> elements)
        {
            if (m_currentElementSequence != null)
            {
                // Search the elements that will be generated next, up to the end of the sequence
                int index = m_currentElementSequence.FindIndex(m_currentIndex, element => elements.Contains(element));
                if (index >= 0)
                {
                    return index - m_currentIndex;
                }
            }

            // TODO: Remember whether we chose the reverse order and use that in GetNextElement
            int index1 = m_originalElementSequence.FindIndex(element => elements.Contains(element));
            int index2 = m_isReversible ? (m_originalElementSequence.Count - 1 - m_originalElementSequence.FindLastIndex(element => elements.Contains(element))) : -1;
            if (index1 >= 0 && index2 >= 0)
            {
                return Math.Min(index1, index2);
            }
            else
            {
                return (index1 >= 0) ? index1 : (index2 >= 0) ? index2 : default(int?);
            }
        }       
    }
}
