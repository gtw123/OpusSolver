using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates atoms of the four cardinal elements from an atom of Quintessence.
    /// </summary>
    public class QuintessenceDisperser : LowCostAtomGenerator
    {
        private static readonly Transform2D InputTransform = new Transform2D(new Vector2(-2, 1), HexRotation.R0);

        private static readonly IReadOnlyDictionary<Element, Transform2D> OutputTransforms = new Dictionary<Element, Transform2D>()
        {
            { Element.Earth, new Transform2D(new Vector2(-3, 2), HexRotation.R60) },
            { Element.Water, new Transform2D(new Vector2(-2, 2), HexRotation.R60) },
            { Element.Fire, new Transform2D(new Vector2(-1, 1), HexRotation.R0) },
            { Element.Air, new Transform2D(new Vector2(-1, 0), HexRotation.R0) },
        };

        public override int RequiredWidth => 3;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [OutputTransforms[Element.Water], OutputTransforms[Element.Fire], OutputTransforms[Element.Earth], OutputTransforms[Element.Air], InputTransform];

        public QuintessenceDisperser(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, new Vector2(-2, 1), HexRotation.R120, GlyphType.Dispersion);
        }

        public override void Consume(Element element, int id)
        {
            ArmController.MoveGrabberTo(InputTransform, this);
            ArmController.DropAtoms(addToGrid: false);

            foreach (var cardinal in PeriodicTable.Cardinals)
            {
                GridState.RegisterAtom(OutputTransforms[cardinal].Position, cardinal, this);
            }
        }

        public override void Generate(Element element, int id)
        {
            var transform = OutputTransforms[element];
            ArmController.MoveGrabberTo(transform, this);
            ArmController.GrabAtoms(new AtomCollection(element, transform, this));
        }
    }
}
