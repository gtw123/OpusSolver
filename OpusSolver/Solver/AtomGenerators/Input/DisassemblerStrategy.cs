using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Input
{
    public class DisassemblyStrategy
    {
        public Molecule Molecule { get; private set; }

        public delegate MoleculeDisassembler CreateDisassemblerDelegate(SolverComponent parent, ProgramWriter writer, Vector2 position);
        public CreateDisassemblerDelegate CreateDisassembler { get; private set; }

        public IEnumerable<Element> ElementInputOrder { get; private set; }

        public DisassemblyStrategy(Molecule molecule, CreateDisassemblerDelegate createDisassembler, IEnumerable<Element> elementInputOrder = null)
        {
            Molecule = molecule;
            CreateDisassembler = createDisassembler;
            ElementInputOrder = elementInputOrder ?? molecule.GetAtomsInInputOrder().Select(a => a.Element);
        }
    }
}