using System;
using System.Collections.Generic;
using System.Linq;

namespace Opus.Solution.Solver.ElementGenerators
{
    public class ReagentGenerator : ElementGenerator
    {
        public override IEnumerable<Element> OutputElements => m_elementSequence;

        public Molecule Reagent { get; private set; }
        public bool Used { get; private set; }

        private List<Element> m_elementSequence;
     
        public ReagentGenerator(CommandSequence commandSequence, Molecule reagent)
            : base(commandSequence)
        {
            Reagent = reagent;
            m_elementSequence = reagent.GetAtomsInInputOrder().Select(a => a.Element).ToList();
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            Used = true;

            CommandSequence.Add(CommandType.Generate, m_elementSequence.First(), this);
            foreach (var element in m_elementSequence.Skip(1))
            {
                AddPendingElement(element);
            }

            return m_elementSequence.First();
        }

        public int? FindClosestElement(IEnumerable<Element> elements)
        {
            // Search the pending elements first
            int currentIndex = (PendingElements > 0) ? m_elementSequence.Count - PendingElements : 0;
            int index = m_elementSequence.FindIndex(currentIndex, element => elements.Contains(element));
            if (index < 0)
            {
                // Search the elements that will be generated when the sequence next wraps around
                index = m_elementSequence.FindIndex(0, currentIndex, element => elements.Contains(element));
            }

            return (index >= 0) ? index : default(int?);
        }

        protected override AtomGenerator CreateAtomGenerator(ProgramWriter writer)
        {
            throw new InvalidOperationException("Reagent atom generators are created by InputGenerator");
        }
    }
}
