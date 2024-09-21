using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public class WasteDisposer : LowCostAtomGenerator, IWasteDisposer
    {
        private static readonly Transform2D DisposalTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [DisposalTransform];

        public WasteDisposer(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, DisposalTransform.Position, HexRotation.R120, GlyphType.Disposal);
        }

        public void Dispose(Element element)
        {
            ArmArea.MoveGrabberTo(DisposalTransform, this);
            ArmArea.DropAtoms(addToGrid: false);
        }
    }
}
