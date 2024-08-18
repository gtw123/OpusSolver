namespace OpusSolver.Solver
{
    /// <summary>
    /// Generates atoms from a reagent molecule by disassembling it (if necessary) into individual atoms.
    /// </summary>
    public abstract class MoleculeDisassembler : SolverComponent
    {
        public override Vector2 OutputPosition => new Vector2(0, 0);

        public Molecule Molecule { get; set; }

        public abstract int Height { get; }
        public virtual int HeightBelowOrigin => 0;

        protected MoleculeDisassembler(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position)
        {
            Molecule = molecule;
        }

        public abstract void GenerateNextAtom();
    }
}
