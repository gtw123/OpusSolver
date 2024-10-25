﻿using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates an atom of Mors and an atom of Vitae from two salt atoms.
    /// </summary>
    public class MorsVitaeGenerator : LowCostAtomGenerator
    {
        private bool m_hasSalt = false;

        private static readonly Transform2D SaltInput1Transform = new Transform2D(new Vector2(-1, 1), HexRotation.R0);
        private static readonly Transform2D SaltInput2Transform = new Transform2D(new Vector2(-1, 0), HexRotation.R0);
        private static readonly Transform2D MorsOutputTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D VitaeOutputTransform = new Transform2D(new Vector2(-2, 1), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [MorsOutputTransform, SaltInput1Transform, VitaeOutputTransform, SaltInput2Transform];

        public MorsVitaeGenerator(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, new Vector2(-1, 0), HexRotation.R60, GlyphType.Animismus);
        }

        public override void Consume(Element element, int id)
        {
            if (!m_hasSalt)
            {
                ArmController.DropMoleculeAt(SaltInput1Transform, this);
                m_hasSalt = true;
            }
            else
            {
                ArmController.DropMoleculeAt(SaltInput2Transform, this, addToGrid: false);
                GridState.UnregisterAtom(SaltInput1Transform.Position, this);

                GridState.RegisterAtom(MorsOutputTransform.Position, Element.Mors, this);
                GridState.RegisterAtom(VitaeOutputTransform.Position, Element.Vitae, this);

                m_hasSalt = false;
            }
        }

        public override void Generate(Element element, int id)
        {
            var transform = element switch
            {
                Element.Mors => MorsOutputTransform,
                Element.Vitae => VitaeOutputTransform,
                _ => throw new SolverException($"{nameof(MorsVitaeGenerator)} can only generate Mors and Vitae but {element} was requested.")
            };

            ArmController.SetMoleculeToGrab(new AtomCollection(element, transform, this));
        }
    }
}
