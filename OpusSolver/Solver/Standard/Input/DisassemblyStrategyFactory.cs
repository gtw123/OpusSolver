using System.Linq;

namespace OpusSolver.Solver.Standard.Input
{
    public static class DisassemblyStrategyFactory
    {
        public static MoleculeDisassemblyStrategy CreateDisassemblyStrategy(Molecule molecule)
        {
            if (molecule.Atoms.Count() == 1)
            {
                return new MoleculeDisassemblyStrategy(molecule, null);
            }
            else if (molecule.Height == 1)
            {
                return new MoleculeDisassemblyStrategy(molecule, (parent, writer, position) => new LinearDisassembler(parent, writer, position, molecule));
            }
            else if (NonLinear3BentDisassembler.IsCompatible(molecule))
            {
                NonLinear3BentDisassembler.PrepareMolecule(molecule);
                return new MoleculeDisassemblyStrategy(molecule, (parent, writer, position) => new NonLinear3BentDisassembler(parent, writer, position, molecule),
                    NonLinear3BentDisassembler.GetElementInputOrder(molecule));
            }
            else if (NonLinear3TriangleDisassembler.IsCompatible(molecule))
            {
                NonLinear3TriangleDisassembler.PrepareMolecule(molecule);
                return new MoleculeDisassemblyStrategy(molecule, (parent, writer, position) => new NonLinear3TriangleDisassembler(parent, writer, position, molecule),
                    NonLinear3TriangleDisassembler.GetElementInputOrder(molecule));
            }

            return new MoleculeDisassemblyStrategy(molecule, (parent, writer, position) => new UniversalDisassembler(parent, writer, position, molecule));
        }
    }
}
