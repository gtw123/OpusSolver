using System;
using System.Collections.Generic;
using System.Linq;
using LPSolve;

namespace OpusSolver.Solver
{
    public class RecipeBuilder
    {
        private readonly List<Reaction> m_reactions = new();
        private readonly List<(Reaction Reaction, int Count)> m_productReactions = new();
        private List<Element> m_usedElements;

        public void AddReagents(IEnumerable<Molecule> reagents)
        {
            // For each reagent, get the total number of atoms of each element
            var reagentElementCountsByID = new Dictionary<int, Dictionary<Element, int>>();
            foreach (var reagent in reagents)
            {
                reagentElementCountsByID[reagent.ID] = reagent.Atoms.Select(a => a.Element).GroupBy(e => e).ToDictionary(g => g.Key, g => g.Count());
            }
            
            // Get all the molecules that have only one type of element. Having more than one of these is redundant,
            // so we can remove some of them.
            var singleElementReagents = reagentElementCountsByID.Where(p => p.Value.Count == 1).Select(p => new {ID = p.Key, Element = p.Value.Keys.First(), AtomCount = p.Value.Values.First()}).ToList();
            foreach (var reagentsWithSameElement in singleElementReagents.GroupBy(p => p.Element))
            {
                // Remove all but the smallest of these. This saves cycles and cost on some solutions.
                foreach (var reagentToRemove in reagentsWithSameElement.OrderBy(r => r.AtomCount).Skip(1))
                {
                    reagentElementCountsByID.Remove(reagentToRemove.ID);
                }
            }
      
            foreach (var (id, elementCounts) in reagentElementCountsByID)
            {
                m_reactions.Add(new Reaction(ReactionType.Reagent, id, new Dictionary<Element, int>(), elementCounts));
            }
        }

        public void AddProducts(IEnumerable<Molecule> products, int outputScale)
        {
            bool anyRepeats = products.Any(product => product.HasRepeats);

            foreach (var product in products)
            {
                // If there's a mix of repeating and non-repeating molecules, we need to build extra copies
                // of the non-repeating ones. This is to compensate for the fact that we build all copies of
                // the repeating molecules at the same time.
                int numCopies = (anyRepeats && !product.HasRepeats) ? 6 * outputScale : 1;
                var elementCounts = product.Atoms.Select(a => a.Element).GroupBy(e => e).ToDictionary(g => g.Key, g => g.Count());
                var reaction = new Reaction(ReactionType.Product, product.ID, elementCounts, new Dictionary<Element, int>());
                m_productReactions.Add((reaction, numCopies));
            }
        }

        public void AddReaction(ReactionType type)
        {
            if (type == ReactionType.Calcification)
            {
                foreach (var element in PeriodicTable.Cardinals)
                {
                    m_reactions.Add(CreateReaction(type, element));
                }
            }
            else if (type == ReactionType.VanBerlo)
            {
                foreach (var element in PeriodicTable.Cardinals)
                {
                    m_reactions.Add(CreateReaction(type, element));
                }
            }
            else if (type == ReactionType.Projection || type == ReactionType.Purification)
            {
                for (int i = 0; i < PeriodicTable.Metals.Count - 1; i++)
                {
                    m_reactions.Add(CreateReaction(type, PeriodicTable.Metals[i]));
                }
            }
            else
            {
                m_reactions.Add(CreateReaction(type));
            }
        }

        private Reaction CreateReaction(ReactionType type, Element? element = null, int id = 0)
        {
            IEnumerable<(Element, int)> inputs, outputs;

            switch (type)
            {
                case ReactionType.Calcification:
                    inputs = [(element.Value, 1)];
                    outputs = [(Element.Salt, 1)];
                    break;
                case ReactionType.VanBerlo:
                    inputs = [(Element.Salt, 1)];
                    outputs = [(element.Value, 1)];
                    break;
                case ReactionType.Animismus:
                    inputs = [(Element.Salt, 2)];
                    outputs = [(Element.Mors, 1), (Element.Vitae, 1)];
                    break;
                case ReactionType.Projection:
                    inputs = [(element.Value, 1), (Element.Quicksilver, 1)];
                    outputs = [(element.Value + 1, 1)];
                    break;
                case ReactionType.Purification:
                    inputs = [(element.Value, 2)];
                    outputs = [(element.Value + 1, 1)];
                    break;
                case ReactionType.Dispersion:
                    inputs = [(Element.Quintessence, 1)];
                    outputs = [(Element.Air, 1), (Element.Fire, 1), (Element.Water, 1), (Element.Earth, 1)];
                    break;
                case ReactionType.Unification:
                    inputs = [(Element.Air, 1), (Element.Fire, 1), (Element.Water, 1), (Element.Earth, 1)];
                    outputs = [(Element.Quintessence, 1)];
                    break;
                default:
                    throw new ArgumentException($"Invalid component type {type}.");
            };

            return new Reaction(type, id, inputs.ToDictionary(p => p.Item1, p => p.Item2), outputs.ToDictionary(p => p.Item1, p => p.Item2));
        }

        public IEnumerable<Recipe> GenerateRecipes()
        {
            m_usedElements = m_reactions.SelectMany(r => r.Inputs.Keys).Concat(m_reactions.SelectMany(r => r.Outputs.Keys))
                .Concat(m_productReactions.SelectMany(r => r.Reaction.Inputs.Keys)).Distinct().OrderBy(e => e).ToList();
            using var lp = CreateLinearProgram();
            return [FindFeasibleRecipe(lp)];
        }

        private LinearProgram CreateLinearProgram()
        {
            var lp = new LinearProgram(m_reactions.Count);

            for (int i = 0; i < m_reactions.Count; i++)
            {
                lp.SetVariableIsInteger(i, true);
            }

            var objValues = new double[m_reactions.Count];
            for (int i = 0; i < m_reactions.Count; i++)
            {
                if (m_reactions[i].Type == ReactionType.Reagent)
                {
                    // Weight reagents by the number of atoms within them. This helps make the solver prioritise
                    // smaller reagents rather than larger ones, which makes more efficient solutions in some cases.
                    objValues[i] = 1.0 * m_reactions[i].Outputs.Sum(c => c.Value);
                }
            }

            lp.SetObjectiveFunction(objValues);
            lp.SetObjectiveType(ObjectiveType.Minimize);

            foreach (var element in m_usedElements)
            {
                var rowValues = new double[m_reactions.Count];
                for (int i = 0; i < m_reactions.Count; i++)
                {
                    m_reactions[i].Inputs.TryGetValue(element, out int inputAtoms);
                    m_reactions[i].Outputs.TryGetValue(element, out int outputAtoms);
                    rowValues[i] = outputAtoms - inputAtoms;
                }

                // We leave the RH value set to 0 for now, as we'll set it later on in SetProductElementCounts
                lp.AddConstraint(rowValues, ConstraintType.EQ, 0);
            }

            return lp;
        }

        private void SetProductElementCounts(LinearProgram lp, int productScale)
        {
            for (int i = 0; i < m_usedElements.Count; i++)
            {
                int totalElementCount = 0;
                foreach (var product in m_productReactions)
                {
                    product.Reaction.Inputs.TryGetValue(m_usedElements[i], out int elementCount);
                    totalElementCount += product.Count * elementCount * productScale;
                }
                lp.SetConstraintValue(i, totalElementCount);
            }
        }

        private SolveResult SolveLinearProgram(LinearProgram lp, int productScale, bool hasWaste, out Recipe recipe)
        {
            SetProductElementCounts(lp, productScale);

            var result = lp.Solve();
            if (result == SolveResult.OPTIMAL)
            {
                recipe = CreateRecipe(lp, productScale);
                recipe.HasWaste = hasWaste;
            }
            else
            {
                recipe = null;
            }

            return result;
        }

        private Recipe FindFeasibleRecipe(LinearProgram lp)
        {
            // First try to solve the LP exactly. We try different numbers of product counts because sometimes
            // building multiple copies of a product lets us use a whole number of reagents which usually leads
            // to a more optimal solution.
            for (int scale = 1; scale <= 6; scale++)
            {
                if (SolveLinearProgram(lp, scale, hasWaste: false, out var recipe) == SolveResult.OPTIMAL)
                {
                    return recipe;
                }
            }

            // Empirical testing shows that relaxing the contraints for cardinal elements sometimes gives a better
            // solution than relaxing all constraints. So we do this first.
            var cardinals = PeriodicTable.Cardinals.Intersect(m_usedElements);
            if (cardinals.Any())
            {
                foreach (var element in cardinals)
                {
                    lp.SetConstraintType(m_usedElements.IndexOf(element), ConstraintType.GE);
                }

                if (SolveLinearProgram(lp, 1, hasWaste: true, out var recipe) == SolveResult.OPTIMAL)
                {
                    return recipe;
                }
            }

            // Relax all constraints
            for (int i = 0; i < m_usedElements.Count; i++)
            {
                lp.SetConstraintType(i, ConstraintType.GE);
            }

            var result = SolveLinearProgram(lp, 1, hasWaste: true, out var recipe2);
            if (result == SolveResult.OPTIMAL)
            {
                return recipe2;
            }

            throw new SolverException($"Could not solve linear program even after relaxing all constraints: solver returned {result}.");
        }

        private Recipe CreateRecipe(LinearProgram lp, int productScale)
        {
            var values = lp.GetVariableValues();
            if (values.Length != m_reactions.Count)
            {
                throw new SolverException($"Linear program returned {values.Length} variables but expected {m_reactions.Count}.");
            }

            var recipe = new Recipe();
            for (int i = 0; i < values.Length; i++)
            {
                recipe.AddReaction(m_reactions[i], (int)values[i]);
            }

            foreach (var product in m_productReactions)
            {
                recipe.AddReaction(product.Reaction, product.Count * productScale);
            }

            return recipe;
        }
    }
}
