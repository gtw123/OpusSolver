using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class MoleculeAssemblyStrategy
    {
        public IEnumerable<Molecule> Products { get; private set; }

        public delegate MoleculeAssembler CreateAssemblerDelegate(SolverComponent parent, ProgramWriter writer);
        public CreateAssemblerDelegate CreateAssembler { get; private set; }

        public delegate IEnumerable<Element> GetProductBuildOrderDelegate(Molecule molecule);
        public GetProductBuildOrderDelegate GetProductBuildOrder { get; private set; }

        public MoleculeAssemblyStrategy(IEnumerable<Molecule> products, CreateAssemblerDelegate createDisassembler, GetProductBuildOrderDelegate getProductBuildOrder = null)
        {
            Products = products;
            CreateAssembler = createDisassembler;
            GetProductBuildOrder = getProductBuildOrder ?? (product => product.GetAtomsInInputOrder().Select(a => a.Element));
        }
    }
}