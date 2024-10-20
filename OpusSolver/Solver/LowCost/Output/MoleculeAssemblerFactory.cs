using OpusSolver.Solver.LowCost.Output.Complex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output
{
    public class MoleculeAssemblerFactory
    {
        private Func<SolverComponent, ProgramWriter, ArmArea, MoleculeAssembler> m_createAssembler;
        private Dictionary<int, IEnumerable<Element>> m_productElementOrders;

        public MoleculeAssembler CreateAssembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea) => m_createAssembler(parent, writer, armArea);
        public SolutionPlan.MoleculeElementInfo GetProductElementInfo(Molecule molecule) => new SolutionPlan.MoleculeElementInfo(m_productElementOrders[molecule.ID]);

        public MoleculeAssemblerFactory(IEnumerable<Molecule> products, SolutionParameterSet paramSet)
        {
            bool reverseElementOrder = paramSet.GetParameterValue(SolutionParameterRegistry.Common.ReverseProductElementOrder);

            if (products.Any(p => p.HasRepeats))
            {
                throw new UnsupportedException("LowCost solver can't currently handle products with repeats.");
            }
            else if (products.All(p => p.Size == 1))
            {
                if (products.Count() <= MonoatomicAssembler.MaxProducts)
                {
                    m_createAssembler = (parent, writer, armArea) => new MonoatomicAssembler(parent, writer, armArea, products);
                }
                else
                {
                    throw new UnsupportedException($"LowCost solver can't currently handle more than {MonoatomicAssembler.MaxProducts} monoatomic products.");
                }
            }
            else
            {
                bool useBreadthFirstSearch = paramSet.GetParameterValue(SolutionParameters.UseBreadthFirstOrderForComplexProducts);
                bool reverseBondTraversalDirection = paramSet.GetParameterValue(SolutionParameters.ReverseProductBondTraversalDirection);
                var builders = ComplexAssembler.CreateMoleculeBuilders(products, reverseElementOrder, useBreadthFirstSearch, reverseBondTraversalDirection);
                m_productElementOrders = products.ToDictionary(p => p.ID, p => builders.Single(b => b.Product.ID == p.ID).GetElementsInBuildOrder());
                m_createAssembler = (parent, writer, armArea) => new ComplexAssembler(parent, writer, armArea, builders);
            }

            m_productElementOrders ??= products.ToDictionary(p => p.ID, p => p.GetAtomsInInputOrder().Select(a => a.Element));
        }
    }
}
