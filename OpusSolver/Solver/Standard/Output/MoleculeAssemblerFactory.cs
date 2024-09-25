using OpusSolver.Solver.Standard.Output.Hex3;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard.Output
{
    public class MoleculeAssemblerFactory
    {
        private Func<SolverComponent, ProgramWriter, MoleculeAssembler> m_createAssembler;
        private Dictionary<int, IEnumerable<Element>> m_productElementOrders;

        public MoleculeAssembler CreateAssembler(SolverComponent parent, ProgramWriter writer) => m_createAssembler(parent, writer);
        public SolutionPlan.MoleculeElementInfo GetProductElementInfo(Molecule molecule) => new SolutionPlan.MoleculeElementInfo(m_productElementOrders[molecule.ID]);

        public MoleculeAssemblerFactory(IEnumerable<Molecule> products)
        {
            if (!products.Any(p => p.HasTriplex))
            {
                if (products.All(p => p.Size == 1))
                {
                    if (products.Count() == 1)
                    {
                        m_createAssembler = (parent, writer) => new SingleMonoatomicAssembler(parent, writer, products);
                    }
                    else
                    {
                        m_createAssembler = (parent, writer) => new MonoatomicAssembler(parent, writer, products);
                    }
                }
                else if (products.All(p => p.IsLinear)) // includes monoatomic products
                {
                    m_createAssembler = (parent, writer) => new LinearAssembler(parent, writer, products);
                }
                else if (products.All(p => Hex3Assembler.IsProductCompatible(p)))
                {
                    var builders = Hex3Assembler.CreateMoleculeBuilders(products);
                    m_createAssembler = (parent, writer) => new Hex3Assembler(parent, writer, builders);
                    m_productElementOrders = products.ToDictionary(p => p.ID, p => builders.Single(b => b.Product.ID == p.ID).GetElementsInBuildOrder());
                }
            }

            m_createAssembler ??= (parent, writer) => new Universal.UniversalAssembler(parent, writer, products);
            m_productElementOrders ??= products.ToDictionary(p => p.ID, p => p.GetAtomsInInputOrder().Select(a => a.Element));
        }
    }
}