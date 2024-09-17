using System.Collections.Generic;

namespace OpusSolver.Solver
{
    public class SolutionPlan(
        Puzzle puzzle,
        Recipe recipe,
        IReadOnlyDictionary<int, IEnumerable<Element>> reagentElementOrders,
        IReadOnlyDictionary<int, IEnumerable<Element>> productElementOrders,
        bool allowPassthroughWithPendingElements)
    {
        public Puzzle Puzzle { get; private set; } = puzzle;
        public Recipe Recipe { get; private set; } = recipe;

        private IReadOnlyDictionary<int, IEnumerable<Element>> m_reagentElementOrders = reagentElementOrders;
        private IReadOnlyDictionary<int, IEnumerable<Element>> m_productElementOrders = productElementOrders;

        public IEnumerable<Element> GetReagentElementOrder(Molecule reagent) => m_reagentElementOrders[reagent.ID];
        public IEnumerable<Element> GetProductElementOrder(Molecule product) => m_productElementOrders[product.ID];

        public bool AllowPassthroughWithPendingElements { get; private set; } = allowPassthroughWithPendingElements;
    }
}