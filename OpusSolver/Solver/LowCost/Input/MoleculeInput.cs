using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost.Input
{
    public class MoleculeInput : SolverComponent
    {
        public ArmArea ArmArea { get; private set; }
        public ArmController ArmController => ArmArea.ArmController;
        public GridState GridState => ArmArea.GridState;

        public Molecule Molecule { get; private set; }

        private static readonly Transform2D InputTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        public IEnumerable<Transform2D> RequiredAccessPoints => [InputTransform];

        public Transform2D MoleculeTransform { get; private set; }

        private readonly Reagent m_reagent;

        public MoleculeInput(SolverComponent parent, ProgramWriter writer, ArmArea armArea, Transform2D transform, Molecule molecule, Transform2D moleculeTransform)
            : base(parent, writer, transform.Position)
        {
            ArmArea = armArea;
            Molecule = molecule;
            Transform.Rotation = transform.Rotation;

            MoleculeTransform = moleculeTransform;
            m_reagent = new Reagent(this, moleculeTransform.Position, moleculeTransform.Rotation, molecule);
        }

        public AtomCollection GrabMolecule()
        {
            Writer.NewFragment();
            var molecule = new AtomCollection(Molecule, MoleculeTransform, this);
            ArmController.SetMoleculeToGrab(molecule);

            return molecule;
        }

        public Atom GetAtomAtPosition(Vector2 localPos)
        {
            var moleculePos = m_reagent.Transform.Inverse().Apply(localPos);
            return m_reagent.Molecule.GetAtom(moleculePos);
        }
    }
}
