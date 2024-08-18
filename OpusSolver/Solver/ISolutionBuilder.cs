using System;
using System.Collections.Generic;

namespace OpusSolver.Solver
{
    public interface ISolutionBuilder
    {
        Func<Molecule, MoleculeDisassemblyStrategy> CreateDisassemblyStrategy { get; }
        Func<IEnumerable<Molecule>, MoleculeAssemblyStrategy> CreateAssemblyStrategy { get; }

        void CreateAtomGenerators(ElementPipeline pipeline);
    }
}
