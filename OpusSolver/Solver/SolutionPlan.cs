using System.Collections.Generic;

namespace OpusSolver.Solver
{
    public class SolutionPlan(
        Puzzle puzzle,
        Recipe recipe,
        SolutionParameterSet paramSet,
        IEnumerable<Molecule> requiredReagents,
        IReadOnlyDictionary<int, SolutionPlan.MoleculeElementInfo> reagentElementInfo,
        IReadOnlyDictionary<int, SolutionPlan.MoleculeElementInfo> productElementInfo,
        bool useSharedElementBuffer,
        bool usePendingElementsInOrder)
    {
        public record class MoleculeElementInfo(IEnumerable<Element> ElementOrder, bool IsElementOrderReversible = false);

        public Puzzle Puzzle { get; private set; } = puzzle;
        public Recipe Recipe { get; private set; } = recipe;
        public SolutionParameterSet SolutionParameters { get; private set; } = paramSet;

        public IEnumerable<Molecule> RequiredReagents { get; private set; } = requiredReagents;

        private IReadOnlyDictionary<int, MoleculeElementInfo> m_reagentElementInfo = reagentElementInfo;
        private IReadOnlyDictionary<int, MoleculeElementInfo> m_productElementInfo = productElementInfo;

        public MoleculeElementInfo GetReagentElementInfo(Molecule reagent) => m_reagentElementInfo[reagent.ID];
        public MoleculeElementInfo GetProductElementInfo(Molecule product) => m_productElementInfo[product.ID];

        public bool UseSharedElementBuffer { get; private set; } = useSharedElementBuffer;
        public bool UsePendingElementsInOrder { get; private set; } = usePendingElementsInOrder;
    }
}