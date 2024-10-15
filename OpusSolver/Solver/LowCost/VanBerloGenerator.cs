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

        public override void BeginSolution()
        {
            RegisterAtoms();
        }

        private void RegisterAtoms()
        {
            foreach (var (dir, element) in m_controller.GetCurrentElements())
            {
                GridState.RegisterAtom(m_wheelArm.Transform.Position + new Vector2(1, 0).RotateBy(dir + HexRotation.R180), element, this);
            }
        }

        public override void Generate(Element element, int id)
        {
            ArmController.MoveMoleculeTo(DuplicatorTransform, this, options: new ArmMovementOptions { AllowDuplication = true });
            m_controller.RotateToElement(element);
            RegisterAtoms();
            ArmController.GrabbedMolecule.GetAtomAtWorldPosition(DuplicatorTransform.Position, this).Element = element;
        }

        public override void PassThrough(Element element)
        {
            if (element == Element.Salt)
            {
                m_controller.RotateToElement(element);
                RegisterAtoms();
            }
            else
            {
                // Assume it's an atom that is unaffected by the glyph of duplication
            }
        }

        public override void EndSolution()
        {
            // If we reset as early as possible then we might end up doing it before an atom passes through
            // this generator. This means it might not be rotated to Salt and so it'll inadvertently convert
            // a cardinal element to Salt. So we instead do it as late as possible. The -1 is just to avoid
            // adding an extra cycle per product.
            Writer.AdjustTime(-1);
            m_controller.Reset(asEarlyAsPossible: false);
            RegisterAtoms();
        }
    }
}
