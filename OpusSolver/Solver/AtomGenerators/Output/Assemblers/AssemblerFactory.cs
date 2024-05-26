using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    public static class AssemblerFactory 
    {
        public static MoleculeAssembler CreateAssembler(AssemblyType type, SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
        {
            return type switch
            {
                AssemblyType.Monoatomic => products.Count() == 1 ?
                    new SingleMonoatomicMoleculeAssembler(parent, writer, products) :
                    new MonoatomicMoleculeAssembler(parent, writer, products),
                AssemblyType.Linear => new LinearMoleculeAssembler(parent, writer, products),
                AssemblyType.Star2 => new Star2MoleculeAssembler(parent, writer, products),
                AssemblyType.Universal => new UniversalMoleculeAssembler(parent, writer, products),
                _ => throw new ArgumentException($"Unknown assemblty type {type}")
            };
        }
    }
}
