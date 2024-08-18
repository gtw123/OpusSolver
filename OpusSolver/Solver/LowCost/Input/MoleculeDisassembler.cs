﻿namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// Generates atoms from a reagent molecule by disassembling it (if necessary) into individual atoms.
    /// </summary>
    public abstract class MoleculeDisassembler : SolverComponent
    {
        public ArmArea ArmArea { get; private set; }
        public Molecule Molecule { get; protected set; }

        protected MoleculeDisassembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, Vector2 position, Molecule molecule)
            : base(parent, writer, position)
        {
            ArmArea = armArea;
            Molecule = molecule;
        }

        public abstract void GenerateNextAtom();
    }
}
