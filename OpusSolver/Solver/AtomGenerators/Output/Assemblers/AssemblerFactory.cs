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
                    new SingleMonoatomicAssembler(parent, writer, products) :
                    new MonoatomicAssembler(parent, writer, products),
                AssemblyType.Linear => new LinearAssembler(parent, writer, products),
                AssemblyType.Star2 => new Star2Assembler(parent, writer, products),
                AssemblyType.Universal => new UniversalAssembler(parent, writer, products),
                _ => throw new ArgumentException($"Unknown assemblty type {type}")
            };
        }
    }
}
