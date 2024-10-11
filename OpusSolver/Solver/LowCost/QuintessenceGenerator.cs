using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates an atom of Quintessence from atoms of the four cardinal elements.
    /// </summary>
    public class QuintessenceGenerator : LowCostAtomGenerator
    {
        private int m_cardinalCount = 0;

        private static readonly Transform2D[] InputTransforms = [
            new Transform2D(new Vector2(-1, 2), HexRotation.R0),
            new Transform2D(new Vector2(-2, 2), HexRotation.R0),
            new Transform2D(new Vector2(-1, 0), HexRotation.R300),
            new Transform2D(new Vector2(0, 0), HexRotation.R0)
        ];
        private static readonly Transform2D OutputTransform = new Transform2D(new Vector2(-1, 1), HexRotation.R0);

        public override int RequiredWidth => 3;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [InputTransforms[3], OutputTransform, InputTransforms[0], InputTransforms[2], InputTransforms[1]];

        public QuintessenceGenerator(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, new Vector2(-1, 1), HexRotation.R0, GlyphType.Unification);
        }

        public override void Consume(Element element, int id)
        {
            ArmController.MoveAtomsTo(InputTransforms[m_cardinalCount], this);
            ArmController.DropAtoms();

            m_cardinalCount++;
            if (m_cardinalCount == 4)
            {
                foreach (var transform in InputTransforms)
                {
                    GridState.UnregisterAtom(transform.Position, this);
                }

                GridState.RegisterAtom(OutputTransform.Position, Element.Quintessence, this);
                m_cardinalCount = 0;
            }
        }

        public override void Generate(Element element, int id)
        {
            ArmController.SetAtomsToGrab(new AtomCollection(element, OutputTransform, this));
        }
    }
}
