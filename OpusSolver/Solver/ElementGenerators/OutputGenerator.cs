using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.ElementGenerators
{
    /// <summary>
    /// Generates outputs (products) from other element generators.
    /// </summary>
    public class OutputGenerator : ElementGenerator
    {
        private IEnumerable<Molecule> m_products;

        public OutputGenerator(CommandSequence commandSequence, SolutionPlan plan)
            : base(commandSequence, plan)
        {
            m_products = plan.Puzzle.Products;
        }

        protected override bool CanGenerateElement(Element element) => false;

        public void GenerateCommandSequence()
        {
            foreach (var product in m_products)
            {
                var elementOrder = Plan.GetProductElementOrder(product);
                int numCopies = Recipe.GetAvailableReactions(ReactionType.Product, id: product.ID).Single().MaxUsages;
                for (int i = 0; i < numCopies; i++)
                {
                    foreach (var element in elementOrder)
                    {
                        CommandSequence.Add(CommandType.Consume, Parent.RequestElement(element), this, product.ID);
                    }
                }
            }
        }

        protected override Element GenerateElement(IEnumerable<Element> possibleElements)
        {
            throw new InvalidOperationException("Can't call GenerateElement on an OutputGenerator.");
        }
    }
}
