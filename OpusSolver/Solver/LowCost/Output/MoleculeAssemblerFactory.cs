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
        public IEnumerable<Element> GetProductElementOrder(Molecule molecule) => m_productElementOrders[molecule.ID];

        public MoleculeAssemblerFactory(IEnumerable<Molecule> products)
        {
            if (products.Any(p => p.HasTriplex))
            {
                throw new UnsupportedException("LowCost solver can't currently handle products with triplex bonds.");
            }
            else if (products.Any(p => p.HasRepeats))
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
            else if (products.All(p => ComplexAssembler.IsProductCompatible(p)))
            {
                var builders = ComplexAssembler.CreateMoleculeBuilders(products);
                m_productElementOrders = products.ToDictionary(p => p.ID, p => builders.Single(b => b.Product.ID == p.ID).GetElementsInBuildOrder());
                m_createAssembler = (parent, writer, armArea) => new ComplexAssembler(parent, writer, armArea, builders);
            }
            else
            {
                throw new UnsupportedException($"LowCost solver can't currently handle molecules with bond loops.");
            }

            m_productElementOrders ??= products.ToDictionary(p => p.ID, p => p.GetAtomsInInputOrder().Select(a => a.Element));
        }
    }
}
