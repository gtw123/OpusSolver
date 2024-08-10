﻿using System.Collections.Generic;
using System.Linq;
using OpusSolver.Solver.AtomGenerators.Input;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates elements from an input (reagent).
    /// </summary>
    public class ElementInput
    {
        public Molecule Molecule { get; private set; }
        public Recipe Recipe { get; private set; }

        public IEnumerable<Element> ElementSequence => m_elementSequence;
        public bool HasPendingElements => m_currentIndex > 0;
        public bool IsUsed { get; private set; }

        public DisassemblyStrategy Strategy { get; private set; }

        private List<Element> m_elementSequence;
        private int m_currentIndex;
     
        public ElementInput(Molecule molecule, Recipe recipe)
        {
            Molecule = molecule;
            Recipe = recipe;

            Strategy = DisassemblerFactory.CreateDisassemblyStrategy(molecule);
            m_elementSequence = Strategy.ElementInputOrder.ToList();
        }

        public Element GetNextElement()
        {
            IsUsed = true;
            var element = m_elementSequence[m_currentIndex];

            m_currentIndex++;
            if (m_currentIndex >= m_elementSequence.Count)
            {
                m_currentIndex = 0;
                Recipe.RecordReactionUsage(ReactionType.Reagent, id: Molecule.ID);

            }

            return element;
        }

        public int? FindClosestElement(IEnumerable<Element> elements)
        {
            // Search the elements that will be generated next, up to the end of the sequence
            int index = m_elementSequence.FindIndex(m_currentIndex, element => elements.Contains(element));
            if (index < 0)
            {
                // Search the elements that will be generated when the sequence next wraps around
                index = m_elementSequence.FindIndex(0, m_currentIndex, element => elements.Contains(element));
            }

            return (index >= 0) ? index : default(int?);
        }       
    }
}
