namespace OpusSolver.Solver.AtomGenerators.Input
{
    /// <summary>
    /// Generates atoms a from a reagent input.
    /// </summary>
    public abstract class MoleculeInput : SolverComponent
    {
        public override Vector2 OutputPosition => new Vector2(0, 0);

        public Molecule Molecule { get; set; }

        public abstract int Height { get; }
        public virtual int HeightBelowOrigin => 0;

        protected MoleculeInput(SolverComponent parent, ProgramWriter writer, Vector2 position, Molecule molecule)
            : base(parent, writer, position)
        {
            Molecule = molecule;
        }

        public abstract Element GetNextAtom();
    }
}
