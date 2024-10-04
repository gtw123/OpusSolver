using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// Generates atoms from a reagent molecule by disassembling it (if necessary) into individual atoms.
    /// </summary>
    public abstract class MoleculeDisassembler : SolverComponent
    {
        public ArmArea ArmArea { get; private set; }
        public ArmController ArmController => ArmArea.ArmController;
        public GridState GridState => ArmArea.GridState;
        public Molecule Molecule { get; protected set; }

        public abstract IEnumerable<Transform2D> RequiredAccessPoints { get; }

        protected MoleculeDisassembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, Vector2 position, Molecule molecule)
            : base(parent, writer, position)
        {
            ArmArea = armArea;
            Molecule = molecule;
        }

        public virtual void BeginSolution() { }

        public abstract void GrabMolecule();

        public abstract void RegisterInputAtoms();
    }
}
