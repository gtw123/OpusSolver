using System;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Input
{
    /// <summary>
    /// Generates atoms from a single monoatomic reagent molecule.
    /// </summary>
    public class SingleMonoatomicDisassembler : MoleculeDisassembler
    {
        public override int Height => 1;
        public Element Element { get; private set; }

        public SingleMonoatomicDisassembler(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position, molecule)
        {
            if (molecule.Atoms.Count() > 1)
            {
                throw new ArgumentException($"{nameof(SingleMonoatomicDisassembler)} can't handle molecules with multiple atoms.");
            }

            Element = molecule.Atoms.First().Element;
            new Reagent(this, new Vector2(0, 0), HexRotation.R0, molecule);
        }

        public override void GenerateNextAtom()
        {
            Writer.NewFragment();
        }
    }
}
