using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// A diassembler which simply grabs a reagent without disassembling it.
    /// </summary>
    public class SimpleDisassembler : MoleculeDisassembler
    {
        private static readonly Transform2D InputTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        public override IEnumerable<Transform2D> RequiredAccessPoints => [InputTransform];

        public Transform2D MoleculeTransform { get; private set; }

        public SimpleDisassembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, Transform2D transform, Molecule molecule, Transform2D moleculeTransform)
            : base(parent, writer, armArea, transform.Position, molecule)
        {
            Transform.Rotation = transform.Rotation;

            MoleculeTransform = moleculeTransform;
            new Reagent(this, moleculeTransform.Position, moleculeTransform.Rotation, molecule);
        }

        public override void BeginSolution()
        {
            RegisterInputAtoms();
        }

        public override void GrabMolecule()
        {
            Writer.NewFragment();
            ArmController.MoveGrabberTo(InputTransform, this);
            ArmController.GrabAtoms(CreateAtomCollection(), removeFromGrid: false);
        }

        private AtomCollection CreateAtomCollection()
        {
            return new AtomCollection(Molecule, MoleculeTransform, this);
        }

        public override void RegisterInputAtoms()
        {
            GridState.RegisterAtoms(CreateAtomCollection());
        }
    }
}
