﻿using System;
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
                throw new SolverException("LowCost solver can't currently handle products with triplex bonds.");
            }
            else if (products.Any(p => p.HasRepeats))
            {
                throw new SolverException("LowCost solver can't currently handle products with repeats.");
            }
            else if (products.All(p => p.Size == 1))
            {
                if (products.Count() <= MonoatomicAssembler.MaxProducts)
                {
                    m_createAssembler = (parent, writer, armArea) => new MonoatomicAssembler(parent, writer, armArea, products);
                }
                else
                {
                    throw new SolverException($"LowCost solver can't currently handle more than {MonoatomicAssembler.MaxProducts} monoatomic products.");
                }
            }
            else if (products.All(p => p.IsLinear)) // includes monoatomic products
            {
                m_createAssembler = (parent, writer, armArea) => new LinearAssembler(parent, writer, armArea, products);
            }
            else
            {
                throw new SolverException("LowCost solver can't currently handle products with more than one atom.");
            }
 
            m_productElementOrders ??= products.ToDictionary(p => p.ID, p => p.GetAtomsInInputOrder().Select(a => a.Element));
        }
    }
}
