using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// Generates atoms from a single monoatomic reagent molecule.
    /// </summary>
    public class SingleMonoatomicDisassembler : MoleculeDisassembler
    {
        public Element Element { get; private set; }

        private static readonly Transform2D InputTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        public override IEnumerable<Transform2D> RequiredAccessPoints => [InputTransform];

        public SingleMonoatomicDisassembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, Transform2D transform, Molecule molecule)
            : base(parent, writer, armArea, transform.Position, molecule)
        {
            Transform.Rotation = transform.Rotation;

            if (molecule.Atoms.Count() > 1)
            {
                throw new ArgumentException($"{nameof(SingleMonoatomicDisassembler)} can't handle molecules with multiple atoms.");
            }

            Element = molecule.Atoms.First().Element;
            new Reagent(this, InputTransform.Position, InputTransform.Rotation, molecule);
        }

        public override void GenerateNextAtom()
        {
            Writer.NewFragment();
            ArmArea.MoveGrabberTo(this, InputTransform);
            ArmArea.GrabAtom();

            // Rotate if necessary to avoid hitting the other reagents
            ArmArea.MoveGrabberTo(this, InputTransform, armRotationOffset: -Transform.Rotation);
        }
    }
}
