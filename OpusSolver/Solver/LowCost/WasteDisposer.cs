using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public class WasteDisposer : LowCostAtomGenerator
    {
        private static readonly Transform2D DisposalTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [DisposalTransform];

        public WasteDisposer(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, DisposalTransform.Position, HexRotation.R120, GlyphType.Disposal);
        }

        public override void Consume(Element element, int id)
        {
            ArmArea.MoveGrabberTo(DisposalTransform, this);
            ArmArea.DropAtoms(addToGrid: false);
        }
    }
}
