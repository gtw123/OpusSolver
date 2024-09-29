using System.Collections.Generic;

namespace OpusSolver.Solver
{
    public class SolutionPlan(
        Puzzle puzzle,
        Recipe recipe,
        IReadOnlyDictionary<int, SolutionPlan.MoleculeElementInfo> reagentElementInfo,
        IReadOnlyDictionary<int, SolutionPlan.MoleculeElementInfo> productElementInfo,
        bool useSharedElementBuffer,
        bool usePendingElementsInOrder)
    {
        public record class MoleculeElementInfo(IEnumerable<Element> ElementOrder, bool IsElementOrderReversible = false);

        public Puzzle Puzzle { get; private set; } = puzzle;
        public Recipe Recipe { get; private set; } = recipe;

        private IReadOnlyDictionary<int, MoleculeElementInfo> m_reagentElementOrders = reagentElementInfo;
        private IReadOnlyDictionary<int, MoleculeElementInfo> m_productElementOrders = productElementInfo;

        public MoleculeElementInfo GetReagentElementInfo(Molecule reagent) => m_reagentElementOrders[reagent.ID];
        public MoleculeElementInfo GetProductElementInfo(Molecule product) => m_productElementOrders[product.ID];

        public bool UseSharedElementBuffer { get; private set; } = useSharedElementBuffer;
        public bool UsePendingElementsInOrder { get; private set; } = usePendingElementsInOrder;
    }
}