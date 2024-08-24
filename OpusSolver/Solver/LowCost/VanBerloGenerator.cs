using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates an atom of a cardinal element from salt using Van Berlo's wheel.
    /// </summary>
    public class VanBerloGenerator : LowCostAtomGenerator
    {
        private readonly Arm m_wheelArm;
        private readonly VanBerloController m_controller;

        private static readonly Transform2D DuplicatorTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [DuplicatorTransform];

        public VanBerloGenerator(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, new Vector2(1, 0), HexRotation.R180, GlyphType.Duplication);
            m_wheelArm = new Arm(this, new Vector2(2, 0), HexRotation.R0, ArmType.VanBerlo);

            m_controller = new VanBerloController(Writer, m_wheelArm);
        }

        public override void Generate(Element element, int id)
        {
            m_controller.RotateToElement(element);

            ArmArea.MoveGrabberTo(this, DuplicatorTransform);
        }

        public override void PassThrough(Element element)
        {
            if (element == Element.Salt)
            {
                m_controller.RotateToElement(element);
                ArmArea.MoveGrabberTo(this, DuplicatorTransform);
            }
            else
            {
                // Assume it's an atom that is unaffected by the glyph of duplication
            }
        }

        public override void EndSolution()
        {
            m_controller.Reset();
        }
    }
}
