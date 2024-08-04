using System;
using System.Collections.Generic;
using System.Linq;
using LPSolve;

namespace OpusSolver.Solver
{
    public class RecipeGenerator
    {
        private readonly List<Reaction> m_reactions = new();
        private Dictionary<Element, int> m_productCoefficients;
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

        public void AddProducts(IEnumerable<Molecule> products, IReadOnlyDictionary<int, int> productCopyCounts)
        {
            var elementCounts = new List<(Element element, int count)>();
            foreach (var product in products)
            {
                int numCopies = productCopyCounts[product.ID];
                elementCounts.AddRange(product.Atoms.GroupBy(p => p.Element).Select(g => (g.Key, g.Count() * numCopies)));
            }

            m_productCoefficients = elementCounts.GroupBy(e => e.element).ToDictionary(g => g.Key, g => g.Sum(x => x.count));
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

        public Recipe GenerateRecipe()
        {
            m_usedElements = m_reactions.SelectMany(r => r.Inputs.Keys).Concat(m_reactions.SelectMany(r => r.Outputs.Keys))
                .Concat(m_productCoefficients.Keys).Distinct().OrderBy(e => e).ToList();
            using var lp = CreateLinearProgram();
            return SolveLinearProgram(lp);
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

                m_productCoefficients.TryGetValue(element, out int productValue);
                lp.AddConstraint(rowValues, ConstraintType.EQ, productValue);
            }

            return lp;
        }

        private Recipe SolveLinearProgram(LinearProgram lp)
        {
            bool hasWaste = false;
            var result = lp.Solve();

            if (result == SolveResult.INFEASIBLE)
            {
                hasWaste = true;

                // Empirical testing shows that relaxing the contraints for cardinal elements sometimes gives a better
                // solution than relaxing all constraints. So we do this first.
                var cardinals = PeriodicTable.Cardinals.Intersect(m_usedElements);
                if (cardinals.Any())
                {
                    foreach (var element in cardinals)
                    {
                        lp.SetContraintType(m_usedElements.IndexOf(element), ConstraintType.GE);
                    }

                    result = lp.Solve();
                }
            }

            if (result == SolveResult.INFEASIBLE)
            {
                for (int i = 0; i < m_usedElements.Count; i++)
                {
                    lp.SetContraintType(i, ConstraintType.GE);
                }

                result = lp.Solve();
                if (result != SolveResult.OPTIMAL)
                {
                    throw new SolverException($"Could not find optimal solution to linear program even after relaxing constraints: solver returned {result}.");
                }
            }
            else if (result != SolveResult.OPTIMAL)
            {
                throw new SolverException($"Could not solve linear program: solver returned {result}.");
            }

            var recipe = CreateRecipe(lp);
            recipe.HasWaste = hasWaste;

            return recipe;
        }

        private Recipe CreateRecipe(LinearProgram lp)
        {
            var values = lp.GetVariableValues();
            if (values.Length != m_reactions.Count)
            {
                throw new InvalidOperationException($"Linear program returned {values.Length} variables but expected {m_reactions.Count}.");
            }

            var recipe = new Recipe();
            for (int i = 0; i < values.Length; i++)
            {
                recipe.AddReaction(m_reactions[i], (int)values[i]);
            }

            return recipe;
        }
    }
}
