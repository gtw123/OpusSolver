using System.Collections.Generic;
using System.Linq;
using OpusSolver.Solver.AtomGenerators.Output.Hex3;
using OpusSolver.Solver.AtomGenerators.Output.Universal;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    public static class AssemblyStrategyFactory
    {
        public static MoleculeAssemblyStrategy CreateAssemblyStrategy(IEnumerable<Molecule> products)
        {
            if (!products.Any(p => p.HasTriplex))
            {
                if (products.All(p => p.Size == 1))
                {
                    if (products.Count() == 1)
                    {
                        return new MoleculeAssemblyStrategy(products, (parent, writer) => new SingleMonoatomicAssembler(parent, writer, products));
                    }
                    else
                    {
                        return new MoleculeAssemblyStrategy(products, (parent, writer) => new MonoatomicAssembler(parent, writer, products));
                    }
                }
                else if (products.All(p => p.IsLinear)) // includes monoatomic products
                {
                    return new MoleculeAssemblyStrategy(products, (parent, writer) => new LinearAssembler(parent, writer, products));
                }
                else if (products.All(p => Hex3Assembler.IsProductCompatible(p)))
                {
                    var builders = Hex3Assembler.CreateMoleculeBuilders(products);
                    return new MoleculeAssemblyStrategy(products, (parent, writer) => new Hex3Assembler(parent, writer, builders),
                        p => builders.Single(b => b.Product.ID == p.ID).GetElementsInBuildOrder());
                }
            }

            return new MoleculeAssemblyStrategy(products, (parent, writer) => new UniversalAssembler(parent, writer, products));
        }
    }
}
