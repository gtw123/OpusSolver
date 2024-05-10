using System.Collections.Generic;
using System.Linq;
using Opus.Solution.Solver.ElementGenerators;

namespace Opus.Solution.Solver
{
    /// <summary>
    /// Represents a sequence of element generators that can be used to generate all the elements
    /// required by a puzzle.
    /// </summary>
    public class ElementPipeline
    {
        /// <summary>
        /// The sequence of element generators, ordered from inputs to outputs.
        /// </summary>
        public IEnumerable<ElementGenerator> ElementGenerators => m_generators;

        /// <summary>
        /// The output generator as the end of the pipeline.
        /// </summary>
        public OutputGenerator OutputGenerator { get; private set; }

        private Puzzle m_puzzle;
        private CommandSequence m_commandSequence;

        private HashSet<Element> m_generatedElements = new HashSet<Element>();
        private HashSet<Element> m_neededElements = new HashSet<Element>();
        private HashSet<Element> m_reagentElements = new HashSet<Element>();
        private bool m_needAnyCardinal = false;
        private List<ElementGenerator> m_generators = new List<ElementGenerator>();

        public ElementPipeline(Puzzle puzzle, CommandSequence commandSequence)
        {
            m_puzzle = puzzle;
            m_commandSequence = commandSequence;
            Build();
        }

        private void Build()
        {
            // For convenience we build the pipeline in reverse order
            AnalyzeProducts();
            AnalyzeQuintessence();
            AnalyzeMorsVitae();
            AnalyzeCardinals();
            AnalyzeSalt();
            AnalyzeCardinalsAgain();
            AnalyzeMetals();
            AnalyzeReagents();
       }

        private void AnalyzeProducts()
        {
            OutputGenerator = new OutputGenerator(m_commandSequence, m_puzzle.Products);
            AddGenerator(OutputGenerator);

            m_reagentElements.UnionWith(m_puzzle.Reagents.SelectMany(p => p.Atoms.Select(a => a.Element)));
            AddGeneratedElements(m_reagentElements);
            AddNeededElements(m_puzzle.Products.SelectMany(p => p.Atoms.Select(a => a.Element)));
        }

        private void AnalyzeQuintessence()
        {
            if (IsElementMissing(Element.Quintessence))
            {
                if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Unification))
                {
                    AddGenerator(new QuintessenceGenerator(m_commandSequence));
                    AddGeneratedElement(Element.Quintessence);
                    AddNeededElements(PeriodicTable.Cardinals);
                }
                else
                {
                    throw new SolverException("This puzzle requires Quintessence to be created but doesn't allow the glyph of unification.");
                }
            }
        }

        private void AnalyzeMorsVitae()
        {
            if (IsAnyElementMissing(PeriodicTable.MorsVitae))
            {
                if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Animismus))
                {
                    AddGenerator(new ElementBuffer(m_commandSequence));
                    AddGenerator(new MorsVitaeGenerator(m_commandSequence));
                    AddGeneratedElements(PeriodicTable.MorsVitae);
                    AddNeededElement(Element.Salt);
                }
                else
                {
                    throw new SolverException("This puzzle requires Mors or Vitae to be created but doesn't allow the glyph of Animismus.");
                }
            }
        }

        private void AnalyzeCardinals()
        {
            if (IsAnyCardinalMissing())
            {
                if (m_puzzle.AllowedMechanisms.Contains(MechanismType.VanBerlo) && m_puzzle.AllowedGlyphs.Contains(GlyphType.Duplication))
                {
                    if (m_reagentElements.Contains(Element.Salt) || m_reagentElements.Intersect(PeriodicTable.Cardinals).Any())
                    {
                        AddGenerator(new VanBerloGenerator(m_commandSequence));
                        AddGeneratedElements(PeriodicTable.Cardinals);
                        AddNeededElement(Element.Salt);
                    }
                }

                // Don't generate an error if we can't use Van Berlo's wheel as we may still be able to use the glyph
                // of dispersion, but we need to check that after analyzing salt.
            }
        }

        private void AnalyzeSalt()
        {
            if (IsElementMissing(Element.Salt))
            {
                if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Calcification))
                {
                    // Use glyph of calcification
                    AddGenerator(new SaltGenerator(m_commandSequence));
                    AddGeneratedElement(Element.Salt);
                    m_needAnyCardinal = true;   // Special case - we need at least one cardinal but don't care which one it is
                }
                else
                {
                    throw new SolverException("This puzzle requires salt to be created but doesn't allow the glyph of calcification.");
                }
            }
        }

        private void AnalyzeCardinalsAgain()
        {
            if (IsAnyCardinalMissing())
            {
                if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Dispersion) && m_reagentElements.Contains(Element.Quintessence))
                {
                    AddGenerator(new ElementBuffer(m_commandSequence));
                    AddGenerator(new QuintessenceDisperserGenerator(m_commandSequence));
                    AddGeneratedElements(PeriodicTable.Cardinals);
                    AddNeededElement(Element.Quintessence);
                }
                else
                {
                    throw new SolverException("This puzzle requires cardinals to be created but either (a) doesn't allow Van Berlo's wheel and the glyph of duplication, or (b) doesn't allow the glyph of dispersion and doesn't have a reagent with a quintessence atom.");
                }
            }
        }

        private void AnalyzeMetals()
        {
            var missing = GetMissingElements(PeriodicTable.Metals);
            if (missing.Any())
            {
                if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Projection) && m_reagentElements.Contains(Element.Quicksilver))
                {
                    // Use glyph of projection + quicksilver
                    AddGenerator(new MetalProjectorGenerator(m_commandSequence));
                    AddGeneratedElements(missing);
                    AddNeededElement(Element.Quicksilver);
                }
                else if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Purification))
                {
                    // Use glyph of purification
                    AddGenerator(new MetalPurifierGenerator(m_commandSequence));
                    AddGeneratedElements(missing);
                }
                else
                {
                    if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Projection))
                    {
                        throw new SolverException("This puzzle requires metals to be promoted but the reagents don't contain any quicksilver for the glyph of projection, and the puzzle doesn't allow the glyph of purification.");
                    }
                    else
                    {
                        throw new SolverException("This puzzle requires metals to be promoted but doesn't allow the glyph of projection or the glyph of purification.");
                    }
                }
            }
        }

        private void AnalyzeReagents()
        {
            AddGenerator(new ElementBuffer(m_commandSequence));
            AddGenerator(new InputGenerator(m_commandSequence, m_puzzle.Reagents));
        }

        private void AddGenerator(ElementGenerator generator)
        {
            m_generators.Insert(0, generator);
            if (m_generators.Count > 1)
            {
                m_generators[1].Parent = generator;
            }
        }

        private void AddGeneratedElement(Element element)
        {
            m_generatedElements.Add(element);
        }

        private void AddGeneratedElements(IEnumerable<Element> elements)
        {
            m_generatedElements.UnionWith(elements);
        }

        private void AddNeededElement(Element element)
        {
            m_neededElements.Add(element);
        }

        private void AddNeededElements(IEnumerable<Element> elements)
        {
            m_neededElements.UnionWith(elements);
        }

        private bool IsElementMissing(Element element)
        {
            return m_neededElements.Contains(element) && !m_generatedElements.Contains(element);
        }

        private bool IsAnyElementMissing(IEnumerable<Element> elements)
        {
            return GetMissingElements(elements).Any();
        }

        private bool IsAnyCardinalMissing()
        {
            return IsAnyElementMissing(PeriodicTable.Cardinals) || m_needAnyCardinal && !m_generatedElements.Intersect(PeriodicTable.Cardinals).Any();
        }

        private IEnumerable<Element> GetMissingElements(IEnumerable<Element> elements)
        {
            return m_neededElements.Except(m_generatedElements).Intersect(elements);
        }
    }
}
