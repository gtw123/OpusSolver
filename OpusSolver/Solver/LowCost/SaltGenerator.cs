using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates an atom of a cardinal element from salt using Van Berlo's wheel.
    /// </summary>
    public class SaltGenerator : LowCostAtomGenerator
    {
        private static readonly Transform2D CalcifierTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D PassThroughTransform = new Transform2D(new Vector2(-1, 1), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [CalcifierTransform, PassThroughTransform];

        public SaltGenerator(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, CalcifierTransform.Position, CalcifierTransform.Rotation, GlyphType.Calcification);
        }

        public override void Generate(Element element, int id)
        {
            ArmController.MoveMoleculeTo(CalcifierTransform, this, options: new ArmMovementOptions { AllowCalcification = true });
            ArmController.GrabbedMolecule.GetAtomAtWorldPosition(CalcifierTransform.Position, this).Element = Element.Salt;
        }
    }
}
