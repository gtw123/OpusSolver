using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates an atom of salt from an atom of a cardinal element.
    /// Optimized version for the case where no cardinal atoms need to be passed through without
    /// being calcified - i.e. every atom can move straight onto the calcification glyph.
    /// </summary>
    public class SaltGeneratorNoCardinalPassThrough : LowCostAtomGenerator
    {
        private static readonly Transform2D CalcifierTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [CalcifierTransform];

        public SaltGeneratorNoCardinalPassThrough(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, CalcifierTransform.Position, CalcifierTransform.Rotation, GlyphType.Calcification);
        }

        public override void BeginSolution()
        {
            GridState.RegisterGlyph(CalcifierTransform.Position, GlyphType.Calcification, this);
        }

        public override void Generate(Element element, int id)
        {
            ArmController.MoveGrabberTo(CalcifierTransform, this, allowCalcification: true);
            ArmController.GrabbedAtoms.GetAtomAtTransformedPosition(CalcifierTransform.Position, this).Element = Element.Salt;
        }
    }
}
