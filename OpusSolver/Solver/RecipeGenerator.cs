﻿using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class RecipeOptions
    {
        /// <summary>
        /// If true, include some reactions that aren't actually strictly necessary to generate the products.
        /// This can lead to better solutions in some cases.
        /// </summary>
        public bool IncludeUneededReactions = false;
    }

    public class RecipeGenerator
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(RecipeGenerator));

        private readonly Puzzle m_puzzle;
        private readonly RecipeOptions m_options;

        private readonly HashSet<Element> m_generatedElements = new HashSet<Element>();
        private readonly HashSet<Element> m_neededElements = new HashSet<Element>();
        private readonly HashSet<Element> m_reagentElements = new HashSet<Element>();
        private bool m_needAnyCardinal = false;

        private RecipeBuilder m_recipeBuilder = new RecipeBuilder();

        public RecipeGenerator(Puzzle puzzle, RecipeOptions options)
        {
            m_puzzle = puzzle;
            m_options = options;
        }

        public IEnumerable<Recipe> GenerateRecipes(bool generateMultiple)
        {
            AnalyzeProductsAndReagents();
            AnalyzeQuintessence();
            AnalyzeMorsVitae();
            AnalyzeCardinals();
            AnalyzeSalt();
            AnalyzeCardinalsAgain();
            AnalyzeMetals();

            return m_recipeBuilder.GenerateRecipes(generateMultiple);
        }

        private void AnalyzeProductsAndReagents()
        {
            AddNeededElements(m_puzzle.Products.SelectMany(p => p.Atoms.Select(a => a.Element)));
            m_recipeBuilder.AddProducts(m_puzzle.Products, m_puzzle.OutputScale);

            m_reagentElements.UnionWith(m_puzzle.Reagents.SelectMany(p => p.Atoms.Select(a => a.Element)));
            AddGeneratedElements(m_reagentElements);
            m_recipeBuilder.AddReagents(m_puzzle.Reagents);
        }

        private void AnalyzeQuintessence()
        {
            if (IsElementMissing(Element.Quintessence))
            {
                if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Unification))
                {
                    AddGeneratedElement(Element.Quintessence);
                    AddNeededElements(PeriodicTable.Cardinals);
                    m_recipeBuilder.AddReaction(ReactionType.Unification);
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
                    m_recipeBuilder.AddReaction(ReactionType.Animismus);
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
                        m_recipeBuilder.AddReaction(ReactionType.VanBerlo);
                        return;
                    }
                }

                // Don't generate an error if we can't use Van Berlo's wheel as we may still be able to use the glyph
                // of dispersion, but we need to check that after analyzing salt.
            }

            if (m_options.IncludeUneededReactions && m_puzzle.AllowedArmTypes.Contains(ArmType.VanBerlo) && m_puzzle.AllowedGlyphs.Contains(GlyphType.Duplication))
            {
                m_recipeBuilder.AddReaction(ReactionType.VanBerlo);
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
                    m_recipeBuilder.AddReaction(ReactionType.Calcification);
                    return;
                }
                else
                {
                    throw new SolverException("This puzzle requires salt to be created but doesn't allow the glyph of calcification.");
                }
            }

            if (m_options.IncludeUneededReactions && m_puzzle.AllowedGlyphs.Contains(GlyphType.Calcification))
            {
                m_recipeBuilder.AddReaction(ReactionType.Calcification);
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
                    m_recipeBuilder.AddReaction(ReactionType.Dispersion);
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
                    m_recipeBuilder.AddReaction(ReactionType.Projection);
                }
                else if (m_puzzle.AllowedGlyphs.Contains(GlyphType.Purification))
                {
                    // Use glyph of purification
                    AddGeneratedElements(missing);
                    m_recipeBuilder.AddReaction(ReactionType.Purification);
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
    }
}
