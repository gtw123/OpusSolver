using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// Generates atoms from a diatomic reagent molecule.
    /// </summary>
    public class DiatomicDisassembler : MoleculeDisassembler
    {
        private static readonly Transform2D InputTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        public override IEnumerable<Transform2D> RequiredAccessPoints => [InputTransform];

        public DiatomicDisassembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, Transform2D transform, Molecule molecule)
            : base(parent, writer, armArea, transform.Position, molecule)
        {
            Transform.Rotation = transform.Rotation;

            if (molecule.Atoms.Count() != 2)
            {
                throw new ArgumentException($"{nameof(DiatomicDisassembler)} can't handle molecules that don't have two atoms");
            }

            new Reagent(this, InputTransform.Position, InputTransform.Rotation, molecule);
        }

        public override void BeginSolution()
        {
            GridState.RegisterAtoms(CreateAtomCollection());
        }

        public override void GenerateNextAtom()
        {
            Writer.NewFragment();
            ArmArea.MoveGrabberTo(InputTransform, this);
            ArmArea.GrabAtoms(CreateAtomCollection(), removeFromGrid: false);
        }

        private AtomCollection CreateAtomCollection()
        {
            return new AtomCollection(Molecule, InputTransform, this);
        }
    }
}
