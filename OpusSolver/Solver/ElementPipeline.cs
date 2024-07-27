using OpusSolver.Solver.ElementGenerators;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
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
        /// The output generator at the end of the pipeline.
        /// </summary>
        public OutputGenerator OutputGenerator { get; private set; }

        private Puzzle m_puzzle;
        private CommandSequence m_commandSequence;
        private ProgramWriter m_writer;

        private Dictionary<int, int> m_productCopyCounts;
        private HashSet<Element> m_generatedElements = new HashSet<Element>();
        private HashSet<Element> m_neededElements = new HashSet<Element>();
        private HashSet<Element> m_reagentElements = new HashSet<Element>();
        private bool m_needAnyCardinal = false;
        private List<ElementGenerator> m_generators = new List<ElementGenerator>();

        private RecipeGenerator m_recipeGenerator = new RecipeGenerator();

        public ElementPipeline(Puzzle puzzle, CommandSequence commandSequence, ProgramWriter writer)
        {
            m_puzzle = puzzle;
            m_commandSequence = commandSequence;
            m_writer = writer;

            Build();
        }

        private void Build()
        {
            CalculateProductCopyCounts();
            AnalyzeProductsAndReagents();
            AnalyzeQuintessence();
            AnalyzeMorsVitae();
            AnalyzeCardinals();
            AnalyzeSalt();
            AnalyzeCardinalsAgain();
            AnalyzeMetals();

            var recipe = m_recipeGenerator.GenerateRecipe();
            AddGenerators(recipe);
        }

        private void CalculateProductCopyCounts()
        {
            bool anyRepeats = m_puzzle.Products.Any(product => product.HasRepeats);
            int GetNumCopies(Molecule product)
            {
                // If there's a mix of repeating and non-repeating molecules, wee need to build extra copies
                // of the non-repeating ones. This is to compensate for the fact that we build all copies of
                // the repeating molecules at the same time.
                return (anyRepeats && !product.HasRepeats) ? 6 * m_puzzle.OutputScale : 1;
            }

            m_productCopyCounts = m_puzzle.Products.ToDictionary(p => p.ID, p => GetNumCopies(p));
        }

        private void AnalyzeProductsAndReagents()
        {
            AddNeededElements(m_puzzle.Products.SelectMany(p => p.Atoms.Select(a => a.Element)));
            m_recipeGenerator.AddProducts(m_puzzle.Products, m_productCopyCounts);

            m_reagentElements.UnionWith(m_puzzle.Reagents.SelectMany(p => p.Atoms.Select(a => a.Element)));
            AddGeneratedElements(m_reagentElements);
            m_recipeGenerator.AddReagents(m_puzzle.Reagents);
        }

        private void AnalyzeQuintessence()
        {
            if (IsElementMissing(Element.Quintessence))
            {
                if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Unification))
                {
                    AddGeneratedElement(Element.Quintessence);
                    AddNeededElements(PeriodicTable.Cardinals);
                    m_recipeGenerator.AddReaction(ReactionType.Unification);
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
                    AddGeneratedElements(PeriodicTable.MorsVitae);
                    AddNeededElement(Element.Salt);
                    m_recipeGenerator.AddReaction(ReactionType.Animismus);
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
                if (m_puzzle.AllowedArmTypes.Contains(ArmType.VanBerlo) && m_puzzle.AllowedGlyphs.Contains(GlyphType.Duplication))
                {
                    if (m_reagentElements.Contains(Element.Salt) || m_reagentElements.Intersect(PeriodicTable.Cardinals).Any())
                    {
                        AddGeneratedElements(PeriodicTable.Cardinals);
                        AddNeededElement(Element.Salt);
                        m_recipeGenerator.AddReaction(ReactionType.VanBerlo);
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
                    AddGeneratedElement(Element.Salt);
                    m_needAnyCardinal = true;   // Special case - we need at least one cardinal but don't care which one it is
                    m_recipeGenerator.AddReaction(ReactionType.Calcification);
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
                    AddGeneratedElements(PeriodicTable.Cardinals);
                    AddNeededElement(Element.Quintessence);
                    m_recipeGenerator.AddReaction(ReactionType.Dispersion);
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
                    AddGeneratedElements(missing);
                    AddNeededElement(Element.Quicksilver);
                    m_recipeGenerator.AddReaction(ReactionType.Projection);
                }
                else if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Purification))
                {
                    // Use glyph of purification
                    AddGeneratedElements(missing);
                    m_recipeGenerator.AddReaction(ReactionType.Purification);
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

        private void AddGenerators(Recipe recipe)
        {
            AddGenerator(new InputGenerator(m_commandSequence, m_puzzle.Reagents, recipe));
            AddGenerator(new ElementBuffer(m_commandSequence, recipe));

            if (recipe.HasAvailableReactions(ReactionType.Purification))
            {
                AddGenerator(new MetalPurifierGenerator(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Projection))
            {
                AddGenerator(new MetalProjectorGenerator(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Dispersion))
            {
                AddGenerator(new QuintessenceDisperserGenerator(m_commandSequence, recipe));
                AddGenerator(new ElementBuffer(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Calcification))
            {
                AddGenerator(new SaltGenerator(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.VanBerlo))
            {
                AddGenerator(new VanBerloGenerator(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Animismus))
            {
                AddGenerator(new MorsVitaeGenerator(m_commandSequence, recipe));
                AddGenerator(new ElementBuffer(m_commandSequence, recipe));
            }

            if (recipe.HasAvailableReactions(ReactionType.Unification))
            {
                AddGenerator(new QuintessenceGenerator(m_commandSequence, recipe));
            }

            OutputGenerator = new OutputGenerator(m_commandSequence, m_writer, m_puzzle.Products, recipe, m_productCopyCounts);
            AddGenerator(OutputGenerator);
        }

        private void AddGenerator(ElementGenerator generator)
        {
            generator.Parent = m_generators.LastOrDefault();
            m_generators.Add(generator);
        }
    }
}
